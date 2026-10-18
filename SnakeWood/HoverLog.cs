using System.Collections.Generic;
using UnityEngine;

namespace SnakeWood;

// A rideable hover log. Control follows the Lox saddle (Sadle): the rider asks the ZDO owner for
// control, the owner records them as the user and hands them ownership, and from then on the
// rider's own game runs the physics. Everyone else just sees the transform ZSyncTransform syncs.
public class HoverLog : MonoBehaviour, Interactable, Hoverable, IDoodadController
{
	public string m_variant = "";
	public Transform m_attachPoint = null!;
	public Vector3 m_detachOffset = new(0f, 0.6f, -1f);
	public float m_maxUseRange = 4f;

	private const float FreeFallMargin = 1.5f;
	private const float RamCooldown = 0.6f;

	private static readonly RaycastHit[] s_hits = new RaycastHit[16];
	private static int s_groundMask;

	private ZNetView m_nview = null!;
	private Rigidbody m_body = null!;
	private HoverLogVariant m_v = null!;

	// Rider input, written by ApplyControlls on the rider's game.
	private Vector3 m_moveInput;
	private Vector3 m_lookDir;
	private bool m_boost;
	private bool m_brake;
	private bool m_hopQueued;

	private float m_yaw;
	private float m_roll;
	private float m_validUserTimer;
	private bool m_haveValidUser;
	private readonly Dictionary<int, float> m_lastRamTime = new();

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		HoverLogVariant? variant = HoverLogVariant.Get(m_variant);
		if (m_nview.GetZDO() == null || variant == null)
		{
			// Placement ghost (or a broken prefab): just a picture, no physics.
			m_body.isKinematic = true;
			enabled = false;
			return;
		}
		m_v = variant;
		m_attachPoint.localPosition = new Vector3(0f, SnakeWoodPlugin.SeatHeight.Value, -0.2f * m_v.Length);
		m_yaw = transform.eulerAngles.y;

		if (s_groundMask == 0)
		{
			s_groundMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");
		}

		m_nview.Register<long>("SnakeWood_RequestControl", RPC_RequestControl);
		m_nview.Register<long>("SnakeWood_ReleaseControl", RPC_ReleaseControl);
		m_nview.Register<bool>("SnakeWood_RequestResponse", RPC_RequestResponse);
	}

	public HoverLogVariant Variant => m_v;

	// ---- Hover text and mounting -------------------------------------------------------------

	public string GetHoverName() => m_v?.Name ?? "";

	public string GetHoverText()
	{
		if (!m_nview.IsValid() || m_v == null || Player.m_localPlayer == null)
		{
			return "";
		}
		if (IsLocalUser())
		{
			return Localization.instance.Localize(m_v.Name + "\n[<color=yellow><b>$KEY_Use</b></color>] Get off");
		}
		if (!InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
		}
		return Localization.instance.Localize(m_v.Name + "\n[<color=yellow><b>$KEY_Use</b></color>] Ride");
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold || !m_nview.IsValid() || character is not Player player)
		{
			return false;
		}
		// Looking at your own log while riding it and pressing Use gets you off, like looking away does.
		if (ReferenceEquals(player.GetDoodadController(), this))
		{
			player.StopDoodadControl();
			return false;
		}
		if (!InUseDistance(player) || player.IsAttached() || player.GetDoodadController() != null)
		{
			return false;
		}
		m_nview.InvokeRPC("SnakeWood_RequestControl", player.GetZDOID().UserID);
		return false;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

	public float GetHoverOffset() => 0f;

	private bool InUseDistance(Humanoid human) =>
		Vector3.Distance(human.transform.position, transform.position) < m_maxUseRange;

	private void RPC_RequestControl(long sender, long userID)
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		CalculateHaveValidUser();
		if (GetUser() == userID || !m_haveValidUser)
		{
			m_nview.GetZDO().Set(ZDOVars.s_user, userID);
			m_nview.InvokeRPC(sender, "SnakeWood_RequestResponse", true);
			m_nview.GetZDO().SetOwner(sender);
		}
		else
		{
			m_nview.InvokeRPC(sender, "SnakeWood_RequestResponse", false);
		}
	}

	private void RPC_ReleaseControl(long sender, long userID)
	{
		if (m_nview.IsOwner() && GetUser() == userID)
		{
			m_nview.GetZDO().Set(ZDOVars.s_user, 0L);
		}
	}

	private void RPC_RequestResponse(long sender, bool granted)
	{
		Player player = Player.m_localPlayer;
		if (player == null)
		{
			return;
		}
		if (!granted)
		{
			player.Message(MessageHud.MessageType.Center, "$msg_inuse");
			return;
		}
		ResetControls();
		m_yaw = transform.eulerAngles.y;
		player.StartDoodadControl(this);
		player.AttachStart(m_attachPoint, gameObject, hideWeapons: false, isBed: false, onShip: false,
			SnakeWoodPlugin.AttachAnimation.Value, m_detachOffset);
	}

	// ---- IDoodadController: the rider's game feeds us its input every frame ---------------------

	public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
	{
		m_moveInput = moveDir;
		m_lookDir = lookDir;
		m_boost = run;
		m_brake = block;
	}

	public void OnUseStop(Player player)
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		m_nview.InvokeRPC("SnakeWood_ReleaseControl", player.GetZDOID().UserID);
		player.AttachStop();
		ResetControls();
	}

	public Component GetControlledComponent() => this;

	public Vector3 GetPosition() => m_attachPoint.position;

	public bool IsValid() => this;

	// Jump is a hop while riding (see JumpHopsPatch); vanilla would treat it as getting off.
	public void QueueHop() => m_hopQueued = true;

	private void ResetControls()
	{
		m_moveInput = Vector3.zero;
		m_lookDir = transform.forward;
		m_boost = false;
		m_brake = false;
		m_hopQueued = false;
	}

	// ---- Users --------------------------------------------------------------------------------

	private long GetUser() => m_nview.IsValid() ? m_nview.GetZDO().GetLong(ZDOVars.s_user, 0L) : 0L;

	private bool IsLocalUser()
	{
		Player player = Player.m_localPlayer;
		if (player == null)
		{
			return false;
		}
		long user = GetUser();
		return user != 0L && user == player.GetZDOID().UserID;
	}

	private bool IsLocallyDriven()
	{
		Player player = Player.m_localPlayer;
		return player != null && ReferenceEquals(player.GetDoodadController(), this) && IsLocalUser();
	}

	// A recorded user only counts while their character is still nearby (same rule as Sadle).
	private void CalculateHaveValidUser()
	{
		m_haveValidUser = false;
		long user = GetUser();
		if (user == 0L)
		{
			return;
		}
		foreach (ZDO zdo in ZNet.instance.GetAllCharacterZDOS())
		{
			if (zdo.m_uid.UserID == user)
			{
				m_haveValidUser = Vector3.Distance(zdo.GetPosition(), transform.position) < 10f;
				break;
			}
		}
	}

	// ---- Physics (owner only) -----------------------------------------------------------------

	private void FixedUpdate()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		float dt = Time.fixedDeltaTime;
		m_validUserTimer -= dt;
		if (m_validUserTimer <= 0f)
		{
			m_validUserTimer = 1f;
			CalculateHaveValidUser();
		}
		bool driving = IsLocallyDriven();
		if (!driving)
		{
			ResetControls();
		}

		m_body.isKinematic = false;
		m_body.useGravity = false;

		Vector3 pos = m_body.position;
		Vector3 vel = m_body.linearVelocity;
		GetSurface(pos, out float surfaceY, out Vector3 surfaceNormal);

		// Vertical: a damped spring toward hover height, or a (scaled) free fall when well above it.
		float hoverHeight = m_v.HoverHeightCfg.Value;
		float error = surfaceY + hoverHeight - pos.y;
		float verticalAccel;
		if (error < -FreeFallMargin)
		{
			verticalAccel = Physics.gravity.y * m_v.FallGravityCfg.Value;
		}
		else
		{
			verticalAccel = Mathf.Clamp(error * 14f - vel.y * 6f, -40f, 40f);
		}
		vel.y += verticalAccel * dt;
		if (m_hopQueued)
		{
			m_hopQueued = false;
			if (error > -FreeFallMargin && m_v.HopSpeedCfg.Value > 0f)
			{
				vel.y = Mathf.Max(vel.y, m_v.HopSpeedCfg.Value);
			}
		}

		// Steering.
		float throttle = Mathf.Clamp(m_moveInput.z, -1f, 1f);
		float side = Mathf.Clamp(m_moveInput.x, -1f, 1f);
		float turnSpeed = m_v.TurnSpeedCfg.Value;
		float turnInput;
		float strafe = 0f;
		if (SnakeWoodPlugin.Steering.Value == SteeringMode.Keys || !driving)
		{
			turnInput = side;
		}
		else
		{
			Vector3 look = m_lookDir;
			look.y = 0f;
			float delta = look.sqrMagnitude > 0.001f
				? Mathf.DeltaAngle(m_yaw, Quaternion.LookRotation(look).eulerAngles.y)
				: 0f;
			turnInput = Mathf.Clamp(delta / 30f, -1f, 1f);
			strafe = side;
		}
		m_yaw += turnInput * turnSpeed * dt;

		// Horizontal: drive along the log's heading, with grip bleeding off sideways slide.
		Quaternion yawRot = Quaternion.Euler(0f, m_yaw, 0f);
		Vector3 forward = yawRot * Vector3.forward;
		Vector3 right = yawRot * Vector3.right;
		float topSpeed = m_v.MaxSpeedCfg.Value * (m_boost && throttle > 0f ? m_v.BoostCfg.Value : 1f);
		float targetForward = throttle >= 0f ? throttle * topSpeed : throttle * m_v.MaxSpeedCfg.Value * 0.4f;
		float targetSide = strafe * m_v.MaxSpeedCfg.Value * 0.5f;
		if (m_brake)
		{
			targetForward = 0f;
			targetSide = 0f;
		}
		float accel = m_v.AccelerationCfg.Value;
		float brake = accel * (m_brake ? 3f : 1.5f);
		float forwardSpeed = Vector3.Dot(vel, forward);
		float sideSpeed = Vector3.Dot(vel, right);
		forwardSpeed = Mathf.MoveTowards(forwardSpeed, targetForward,
			(Mathf.Abs(targetForward) > Mathf.Abs(forwardSpeed) && Mathf.Sign(targetForward) == Mathf.Sign(forwardSpeed) ? accel : brake) * dt);
		sideSpeed = Mathf.MoveTowards(sideSpeed, targetSide, (targetSide != 0f ? accel : m_v.Grip) * dt);
		Vector3 horizontal = forward * forwardSpeed + right * sideSpeed;
		m_body.linearVelocity = new Vector3(horizontal.x, vel.y, horizontal.z);

		// Rotation: heading, a lean into turns, and a partial tilt to match the ground.
		float targetRoll = -turnInput * Mathf.Clamp01(Mathf.Abs(forwardSpeed) / Mathf.Max(1f, m_v.MaxSpeedCfg.Value)) * 18f;
		m_roll = Mathf.Lerp(m_roll, targetRoll, 4f * dt);
		Vector3 up = Vector3.Slerp(Vector3.up, surfaceNormal, error > -FreeFallMargin ? 0.6f : 0f);
		Quaternion target = Quaternion.FromToRotation(Vector3.up, up) * yawRot * Quaternion.Euler(0f, 0f, m_roll);
		m_body.MoveRotation(Quaternion.Slerp(m_body.rotation, target, 8f * dt));
		m_body.angularVelocity = Vector3.zero;
	}

	// Height and normal of whatever's under the log: ground, build pieces, or the sea surface.
	private void GetSurface(Vector3 pos, out float height, out Vector3 normal)
	{
		height = float.NegativeInfinity;
		normal = Vector3.up;
		int count = Physics.RaycastNonAlloc(pos + Vector3.up * 1f, Vector3.down, s_hits, 60f, s_groundMask, QueryTriggerInteraction.Ignore);
		float best = float.MaxValue;
		for (int i = 0; i < count; i++)
		{
			RaycastHit hit = s_hits[i];
			if (hit.collider.attachedRigidbody == m_body || hit.distance >= best)
			{
				continue;
			}
			best = hit.distance;
			height = hit.point.y;
			normal = hit.normal;
		}
		if (float.IsNegativeInfinity(height) && ZoneSystem.instance != null)
		{
			height = ZoneSystem.instance.GetGroundHeight(pos);
		}
		if (ZoneSystem.instance != null && ZoneSystem.instance.m_waterLevel > height)
		{
			height = ZoneSystem.instance.m_waterLevel;
			normal = Vector3.up;
		}
	}

	// ---- Ramming --------------------------------------------------------------------------------

	private void OnCollisionEnter(Collision collision)
	{
		if (m_v == null || !m_nview.IsValid() || !m_nview.IsOwner() || !IsLocallyDriven())
		{
			return;
		}
		float damage = m_v.RamDamageCfg.Value;
		float speed = collision.relativeVelocity.magnitude;
		float minSpeed = Mathf.Max(3f, m_v.MaxSpeedCfg.Value * 0.35f);
		if (damage <= 0f || speed < minSpeed)
		{
			return;
		}
		IDestructible? destructible = collision.collider.GetComponentInParent<IDestructible>();
		if (destructible is not Component target || target.transform.IsChildOf(transform))
		{
			return;
		}
		Player rider = Player.m_localPlayer;
		if (target.gameObject == rider.gameObject)
		{
			return;
		}
		if (target is Character character && (character.IsPlayer() || character.IsTamed()) && !SnakeWoodPlugin.RamFriendlies.Value)
		{
			return;
		}
		if (target is WearNTear && !SnakeWoodPlugin.RamBuildings.Value)
		{
			return;
		}
		int id = target.GetInstanceID();
		if (m_lastRamTime.TryGetValue(id, out float last) && Time.time - last < RamCooldown)
		{
			return;
		}
		m_lastRamTime[id] = Time.time;

		float topSpeed = m_v.MaxSpeedCfg.Value * m_v.BoostCfg.Value;
		float factor = Mathf.Lerp(0.3f, 1f, Mathf.InverseLerp(minSpeed, topSpeed, speed));
		float amount = damage * factor;
		HitData hit = new();
		hit.m_damage.m_blunt = amount;
		hit.m_damage.m_chop = amount * m_v.RamChop;
		hit.m_damage.m_pickaxe = amount * m_v.RamPickaxe;
		hit.m_damage.m_fire = m_v.RamFire * factor;
		hit.m_damage.m_poison = m_v.RamPoison * factor;
		hit.m_toolTier = (short)m_v.RamToolTier;
		hit.m_pushForce = m_v.RamPush * factor;
		hit.m_point = collision.contactCount > 0 ? collision.GetContact(0).point : target.transform.position;
		Vector3 dir = m_body.linearVelocity;
		dir.y = 0f;
		hit.m_dir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
		hit.SetAttacker(rider);
		destructible.Damage(hit);
	}

	private void OnDestroy()
	{
		Player player = Player.m_localPlayer;
		if (player != null && ReferenceEquals(player.GetDoodadController(), this))
		{
			player.StopDoodadControl();
			player.AttachStop();
		}
	}
}
