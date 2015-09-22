using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HeliControl", "Koenrad", "1.0.6",  ResourceId = 1348)]
    class HeliControl : RustPlugin
    {			
		string[] weaponList = {"rifle.bolt", "pistol.semiauto", "pistol.revolver", "pistol.eoka", "rifle.ak", "smg.2", "smg.thompson" };
		/*--------------------------------------------------------------//
		//			Load up the default config on first use				//
		//--------------------------------------------------------------*/
		protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");
            Config.Clear();
            Config["DisableHeli"] = false;
            Config["ModifyDamageToHeli"] = true;
            Config["UseGlobalDamageModifier"] = true;
			Config["UsePermissions"] = false;
			Config["GlobalDamageMultiplier"] = 4.0;
			Config["HeliBulletDamageAmount"] = 20.0;
			Config["MaxLootCratesToDrop"] = 4;
			Config["HeliSpeed"] = 25.0;
			Config["HeliAccuracy"] = 25.0;
			Config["rifle.bolt"] = 8.0;
			Config["pistol.semiauto"] = 5.0;
			Config["pistol.revolver"] = 5.0;
			Config["pistol.eoka"] = 5.0;
			Config["rifle.ak"] = 5.0;
			Config["smg.2"] = 5.0;
			Config["smg.thompson"] = 5.0;
            SaveConfig();
        }
		/*--------------------------------------------------------------//
		//					OnEntitySpawned Hook						//
		//--------------------------------------------------------------*/
		void OnEntitySpawned(BaseNetworkable entity)
		{
			//994850627 is the prefabID of a heli.
			if (entity == null) return;
			if (entity.prefabID == 994850627)
			{
				if ((bool)Config["DisableHeli"])
				{
					entity.KillMessage();
					Puts("Helicopter destroyed!");
					return;
				}
				BaseHelicopter heli = (BaseHelicopter)entity;
				heli.maxCratesToSpawn = (int)Config["MaxLootCratesToDrop"];
				heli.bulletDamage = float.Parse(Config["HeliBulletDamageAmount"].ToString());
			}
		}
		/*--------------------------------------------------------------//
		//						OnPlayerAttack Hook						//
		//--------------------------------------------------------------*/
		void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
		{
			//994850627 is the prefabID of a Heli.
			if (attacker == null || hitInfo == null || hitInfo.HitEntity == null) return;
			if (hitInfo.HitEntity.prefabID == 994850627 )			//We hit a helicopter
			{
				if (!(bool)Config["ModifyDamageToHeli"]) return;	//Check if damage modification is on
				if ((bool)Config["UseGlobalDamageModifier"])		//Check for global modifier
				{ 
					hitInfo.damageTypes.ScaleAll(float.Parse(Config["GlobalDamageMultiplier"].ToString()));
					return;
				}
				
				string weaponName = hitInfo.Weapon.GetItem().info.shortname;	//weapon's shortname
				if (weaponList.Contains(weaponName))
				{
					hitInfo.damageTypes.ScaleAll(float.Parse(Config[weaponName].ToString()));
				}
				else
				{
					hitInfo.damageTypes.ScaleAll(float.Parse(Config["GlobalDamageMultiplier"].ToString()));
				}
			}
		}
		
		/*--------------------------------------------------------------//
		//						Initial Setup							//
		//--------------------------------------------------------------*/
		void Init()
        {
            if (Config["UsePermissions"] == null)	//This config entry was added 1.0.3, so update the old config for backwards compat.
			{
				Config["UsePermissions"] = false;
				SaveConfig();
			}
			if (Config["HeliSpeed"] == null)	//This config entry was added 1.0.4, so update the old config for backwards compat.
			{
				Config["HeliSpeed"] = 25.0; 
				SaveConfig();
			}
			if (Config["HeliAccuracy"] == null)	//This config entry was added 1.0.4, so update the old config for backwards compat.
			{
				Config["HeliAccuracy"] = 2.0; 
				SaveConfig();
			}
			
			heli.bulletAccuracy = float.Parse(Config["HeliAccuracy"].ToString());
			
			if ((bool)Config["UsePermissions"])			//if userpermissions in config is true register new permission
			{
				if (!permission.PermissionExists("CanCallHeli"))
				{
					permission.RegisterPermission("CanCallHeli", this);
					Puts("CanCallHeli permission registered");
					Puts("Only users with CanCallHeli permission can use the callheli command");
				}
			}
			else	//not using permissions system
			{
				Puts("Not using permissions system,");
				Puts("Admins can use /callheli");
			}
        }
		
		bool IsAllowed(BasePlayer player, string perm)		//self explanitory?
        {
            if (permission.UserHasPermission(player.userID.ToString(), perm)) return true;
            return false;
        }
		
		/*--------------------------------------------------------------//
		//					Chat Command for callheli					//
		//--------------------------------------------------------------*/
		[ChatCommand("callheli")] 
        private void cmdCallToPlayer(BasePlayer player, string command, string[] args)
		{
			if ((bool)Config["UsePermissions"])	//check if permissions system is in use
			{
				if (!IsAllowed(player, "CanCallHeli")) 
				{
					SendReply(player, "You do not have permission for this command.");
					return;
				}
			}
			else
			{
				if (!player.IsAdmin())			//if permissions system is not in use, check for admin
				{
					SendReply(player, "You do not have access to this command.");
					return;
				}
			}
			if (args.Length == 0) 
			{
				call();
				SendReply(player, "Helicopter called.");
				return;
			}
			if (args.Length != 1) 
			{
				SendReply(player, "You need to specify a player name to call the helicopter to");
				return;
			}
			BasePlayer target = FindPlayerByPartialName(args[0]);
			if (target == null) 
			{
				SendReply(player, "Could not find the specified player \"" + args[0] + "\".");
				return;
			}
			callOther(target);
			SendReply(player, "Helicopter called on " + target.displayName);
		}
		
		/*--------------------------------------------------------------//
		//				Console Command for callheli					//
		//--------------------------------------------------------------*/
		[ConsoleCommand("callheli")]
        private void consoleCallHeli(ConsoleSystem.Arg arg)
        {
			if (arg.Args == null || arg.Args.Length == 0 )
			{
				call();
				Puts("Helicopter called!");
				return;
			}
			if (arg.Args.Length != 1) 
			{
				Puts("You need to specify a player name to call the helicopter to");
				return;
			}
			BasePlayer target = FindPlayerByPartialName(arg.Args[0]);
			if (target == null) 
			{
				Puts("Could not find the specified player \"" + arg.Args[0] + "\".");
				return;
			}
			callOther(target);
			Puts("Helicopter called on " + target.displayName);
        }
		
		/*--------------------------------------------------------------//
		//					Chat Command for killheli					//
		//--------------------------------------------------------------*/
		[ChatCommand("killheli")] 
        private void cmdKillHeli(BasePlayer player, string command, string[] args)
		{
			if ((bool)Config["UsePermissions"])	//check if permissions system is in use
			{
				if (!IsAllowed(player, "CanCallHeli")) 
				{
					SendReply(player, "You do not have permission for this command.");
					return;
				}
			}
			else
			{
				if (!player.IsAdmin())			//if permissions system is not in use, check for admin
				{
					SendReply(player, "You do not have access to this command.");
					return;
				}
			}
			killAll();
		}
		
		/*--------------------------------------------------------------//
		//				Console Command for killheli					//
		//--------------------------------------------------------------*/
		[ConsoleCommand("killheli")]
        private void consoleKillHeli(ConsoleSystem.Arg arg)
        {
			killAll();
		}
		
		/*--------------------------------------------------------------//
		//				kill all produces no loot drops					//
		//--------------------------------------------------------------*/
		private void killAll()
		{
			BaseHelicopter[] allHelicopters = UnityEngine.Object.FindObjectsOfType<BaseHelicopter>();
			foreach (BaseHelicopter helicopter in allHelicopters) {
				helicopter.maxCratesToSpawn = 0;		//comment this line if you want loot drops with killheli
				helicopter.DieInstantly();
			}
			Puts("All helicopters were annihilated");
		}
		
		/*--------------------------------------------------------------//
		//				call heli on other person						//
		//--------------------------------------------------------------*/
		private void callOther(BasePlayer target)
		{
			BaseEntity entity = GameManager.server.CreateEntity("assets/bundled/prefabs/npc/patrol_helicopter/PatrolHelicopter.prefab", new Vector3(), new Quaternion(), true);
			if (!(bool) ((UnityEngine.Object) entity)) return;
			PatrolHelicopterAI heliAI = entity.GetComponent<PatrolHelicopterAI>();
			heliAI.maxSpeed = float.Parse(Config["HeliSpeed"].ToString());		//helicopter speed
			entity.GetComponent<PatrolHelicopterAI>().SetInitialDestination(target.transform.position + new Vector3(0.0f, 10f, 0.0f), 0.25f);
			entity.Spawn(true);
			
		}
		
		/*--------------------------------------------------------------//
		//					call heli in general						//
		//--------------------------------------------------------------*/
		private void call()
		{
			BaseEntity entity = GameManager.server.CreateEntity("assets/bundled/prefabs/npc/patrol_helicopter/PatrolHelicopter.prefab", new Vector3(), new Quaternion(), true);
			if (!(bool) ((UnityEngine.Object) entity))
				return;
			PatrolHelicopterAI heliAI = entity.GetComponent<PatrolHelicopterAI>();
			heliAI.maxSpeed = float.Parse(Config["HeliSpeed"].ToString());		//helicopter speed
			entity.Spawn(true);
		}

		/*--------------------------------------------------------------//
		//				Thank You Whoever Wrote This					//
		//--------------------------------------------------------------*/
		// Finds a player by partial name
        private BasePlayer FindPlayerByPartialName(string name) {
            if (string.IsNullOrEmpty(name))
                return null;
            BasePlayer player = null;
            name = name.ToLower();
            var allPlayers = BasePlayer.activePlayerList.ToArray();
            // Try to find an exact match first
            foreach (var p in allPlayers) {
                if (p.displayName == name) {
                    if (player != null)
                        return null; // Not unique
                    player = p;
                }
            }
            if (player != null)
                return player;
            // Otherwise try to find a partial match
            foreach (var p in allPlayers) {
                if (p.displayName.ToLower().IndexOf(name) >= 0) {
                    if (player != null)
                        return null; // Not unique
                    player = p;
                }
            }
            return player;
        }

	}
}