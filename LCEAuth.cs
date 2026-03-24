using Minecraft.Server.FourKit;
using Minecraft.Server.FourKit.Plugin;
using Minecraft.Server.FourKit.Event;
using Minecraft.Server.FourKit.Event.Player;
using Minecraft.Server.FourKit.Event.Entity;
using Minecraft.Server.FourKit.Command;
using Minecraft.Server.FourKit.Event.Block;
using Minecraft.Server.FourKit.Event.Inventory;
using Minecraft.Server.FourKit.Entity;

using System.Security.Cryptography;

namespace LCEAuth;

public class LCEAuth : ServerPlugin
{

	public void OnEnable() {
		FourKit.addListener(new AuthListener());
		FourKit.getCommand("auth").setExecutor(new Auth());
		FourKit.getCommand("areg").setExecutor(new Areg());
		FourKit.getCommand("authadmin").setExecutor(new AAdmin());
	}

	public void OnDisable() {
		Database.Instance.Dispose();
	}

	public override string name => "LCEAuth";
	public override string version => "0.3";
	public override string author => "UniPM";
}

public class PlayerDB
{
	public LiteDB.ObjectId Id { get; set; } = LiteDB.ObjectId.NewObjectId(); // pretty much unused and only to prevent issues - uni
	public string? Name { get; set; }
	public string? passCrypt { get; set; }
	public Location? coords { get; set; }
	public string? ipAddress { get; set; }
	public DateTime? ipReset { get; set; }
	public Guid? uid { get; set; }
}

public class Database : IDisposable
{
    private static Database? _instance;
    private readonly LiteDB.LiteDatabase _db;

    private Database()
    {
        _db = new LiteDB.LiteDatabase(@".\plugindb\LCEAUTH_DB_DO_NOT_MODIFY.db");
    }

    public static Database Instance => _instance ??= new Database();

    public LiteDB.ILiteCollection<T> GetCollection<T>(string name)
        => _db.GetCollection<T>(name);

    public void Dispose()
    {
        _db.Dispose();
        _instance = null;
    }
} // srry guys this singleton is vibecoded :( - uni

public class AuthListener : Listener
{
	public static bool testPass(string plrname, string pass)
	{
		if (string.IsNullOrEmpty(plrname) || string.IsNullOrEmpty(pass)) return false;

		var col = Database.Instance.GetCollection<PlayerDB>("playerdb");

		var getPlr = col.Find(LiteDB.Query.EQ("Name", plrname))
			.Select(x => new {GetCrypt = x.passCrypt})
			.FirstOrDefault();

		if (getPlr != null && getPlr.GetCrypt != null && BCrypt.Net.BCrypt.Verify(pass, getPlr.GetCrypt)) return true; // bcrypt.net-next is really fucking weird - uni
		return false;
	}

	public static bool isReal(string plrname)
	{
		if (string.IsNullOrEmpty(plrname)) return false;
		var col = Database.Instance.GetCollection<PlayerDB>("playerdb");

		bool getPlr = col.Exists(LiteDB.Query.EQ("Name", plrname));
		return getPlr;
	}

	public static void createPlr(string plrname, string pass)
	{
		if (string.IsNullOrEmpty(plrname) || string.IsNullOrEmpty(pass)) return;
		if (isReal(plrname)) return;
		var col = Database.Instance.GetCollection<PlayerDB>("playerdb");

		var newplr = new PlayerDB
		{
			Name = plrname,
			passCrypt = BCrypt.Net.BCrypt.HashPassword(pass)
		};

		col.Insert(newplr);
	}

	public static PlayerDB? getPlrDB(string plrname)
	{
		if (string.IsNullOrEmpty(plrname)) return null;
		var col = Database.Instance.GetCollection<PlayerDB>("playerdb");

		var getPlr = col.Find(LiteDB.Query.EQ("Name", plrname))
			.FirstOrDefault();

		return getPlr;
	}

	public static bool addressCheck(Player plr)
	{
		if (plr == null) return false;
		if (!isReal(plr.getName())) return false;
		var col = Database.Instance.GetCollection<PlayerDB>("playerdb");

		var getPlr = col.Find(LiteDB.Query.EQ("Name", plr.getName()))
			.Select(x => new {GetIP = x.ipAddress})
			.FirstOrDefault();

		return (getPlr != null && getPlr.GetIP != null && BCrypt.Net.BCrypt.Verify(plr!.getAddress().getAddress().getHostAddress(), getPlr.GetIP)); // so long (thats what she said) - uni
	}

//	public static void tpPlr(Player plr)
//	{
//		var col = Database.Instance.GetCollection<PlayerDB>("playerdb");
//		var getPlr = col.FindOne(LiteDB.Query.EQ("Name", plr.getName()));
//		if (getPlr?.coords != null)
//		{
//			plr.teleport(getPlr.coords);
//		}
//	}

	public static List<string> unauthedUsrs = // god i fucking hate c# sometimes - uni
		new List<string>();

	public static void AuthInit(Player plr)
	{
		// username change code - uni
		var col1 = Database.Instance.GetCollection<PlayerDB>("playerdb"); // unique name ikr - uni
                var plrCheckName = col1.Find(LiteDB.Query.EQ("uid", plr.getUniqueId()));
		
		foreach (PlayerDB playr in plrCheckName)
		{
			if (isReal(plr.getName()))
			{
				break; // the last thing you want is a broken db is what i always say :3 - uni
			}
			if (playr.Name != plr.getName())
			{
				playr.Name = plr.getName(); // set username
				col1.Update(playr); // i have pre-act tmrw and i wanna die - uni
				break; // one potential problem i see with this is someone creating 3 accs (somehow), logging into one and then logging into the other. Would that not create two accounts with the same name??? - uni
				// fixed the problem above using an israel check ^ - uni
			}
		}

		if (addressCheck(plr)) {
			plr.sendMessage($"Logged in as {plr.getName()}");
			Console.WriteLine($"PlayerDB Location for {plr.getName()}: {getPlrDB(plr.getName()).coords}");
			return;
		}
//		var col = Database.Instance.GetCollection<PlayerDB>("playerdb");
//		var coords = col.Find(LiteDB.Query.EQ("Name", plr.getName())).Select(x => new {coordins = x.coords}).FirstOrDefault()?.coordins;
//		if (isReal(plr.getName()) && coords != null)
//		{
//			plr.teleport(new Location(coords.getWorld(), 0, 1000, 0));
//		}
		unauthedUsrs.Add(plr.getName());
		plr.sendMessage("LCEAuth");
        	plr.sendMessage("Type password to continue. /auth <password>"); // [TODO] add 30s wait - uni (idk when tf i'll ever add this - uni 3/23/26)
		plr.sendMessage("Optionally, you can input '/auth <password> -noip' to prevent IP tracking.");
//		if (getPlrDB(plr.getName())?.uid == null) { plr.sendMessage("You can also input '/auth <password -nouid>' to prevent UID tracking. You can combine both flags to get '-noip-nouid'."); }
		if (!isReal(plr.getName())) plr.sendMessage("No account found! Register with /areg <password> <confirmpassword>");
	}

	public static bool AuthFinish(Player plr, string[] args)
	{
		if (!(args.Length >= 1)) return false;
		if (!isReal(plr.getName())) return false;
		if (testPass(plr.getName(), args[0])) {
			unauthedUsrs.Remove(plr.getName());
			var col = Database.Instance.GetCollection<PlayerDB>("playerdb");

			var getPlr = col.Find(LiteDB.Query.EQ("Name", plr.getName()))
				.FirstOrDefault();
			
			if (getPlr == null) return false;
			if (args.Length == 2) {
				if (!args[1].Contains("-noip")) getPlr.ipAddress = BCrypt.Net.BCrypt.HashPassword(plr!.getAddress().getAddress().getHostAddress()); // why the hell is it like this?? - uni
//				if (!args[1].Contains("-nouid") && getPlr.uid == null) getPlr.uid = plr.getUniqueId(); // so much more simple than ^ this shit - uni
			} else {
				getPlr.ipAddress = BCrypt.Net.BCrypt.HashPassword(plr!.getAddress().getAddress().getHostAddress()); // why the hell is it like this?? - uni
//                              getPlr.uid = plr.getUniqueId(); // so much more simple than ^ this shit - uni
//                              don't ever think about adding this shit again - uni
			} // copying and pasting is my passion - uni
//			tpPlr(plr);

			col.Update(getPlr);
			// hashed for better protection :> - uni
			return true;
		}
		return false;
	}

	public static bool cmdRecover(string[] args)
	{
		if (args.Length != 2) return false;

		if (string.IsNullOrEmpty(args[1])) { Console.WriteLine("[AAuth] at /authadmin: missing arg Player"); return false; }
                if (!AuthListener.isReal(args[1])) { Console.WriteLine($"[AAuth] at /authadmin: Player {args[1]} is not registered!"); return false; }
                string newPass = AAdmin.passGen(); // gen new pass - uni

                var col = Database.Instance.GetCollection<PlayerDB>("playerdb");
                var getPlr = col.Find(LiteDB.Query.EQ("Name", args[1])).FirstOrDefault();
		
		if (getPlr == null) { Console.WriteLine($"[AAuth] getPlr failed: null"); return false; }

                getPlr.passCrypt = BCrypt.Net.BCrypt.HashPassword(newPass);
		
		getPlr.ipAddress = null;

                col.Update(getPlr);

                if (!AuthListener.testPass(args[1], newPass)) return false;
		
		FourKit.getPlayer(args[1]).kickPlayer();
                Console.WriteLine($"[AAuth] Recovered {args[1]}! New password: {newPass}");
                return true;
	}

	public static bool cmdChPass(string[] args) // I FUCKING HATE LITEDB!!! - uni
	{
		if (args.Length != 3) return false;

		if (string.IsNullOrEmpty(args[1])) { Console.WriteLine("[AAuth] at /authadmin: missing arg Player"); return false; }
                if (!AuthListener.isReal(args[1])) { Console.WriteLine($"[AAuth] at /authadmin: Player {args[1]} is not registered!"); return false; }

                var col = Database.Instance.GetCollection<PlayerDB>("playerdb");
                var getPlr = col.Find(LiteDB.Query.EQ("Name", args[1])).FirstOrDefault();
		
		if (getPlr == null) { Console.WriteLine($"[AAuth] getPlr failed: null"); return false; }

                getPlr.passCrypt = BCrypt.Net.BCrypt.HashPassword(args[2]);
		
		getPlr.ipAddress = null;

                col.Update(getPlr);

                if (!AuthListener.testPass(args[1], args[2])) return false;
		
		FourKit.getPlayer(args[1]).kickPlayer();
                Console.WriteLine($"[AAuth] Changed {args[1]}'s password. New password: {args[2]}");
                return true;

	}
	//  |   _ _    _ _ _  _ ___ _   _ _  _  _  _ _  _   _ __   |
	//  |  |_  \  / |_ |\ |  | /    |_| /_\ |\ | |\ |  |_ |_\  |
	// \|/ |_   \/  |_ | \|  | _\   | | | | | \| |/ |_ |_ | \ \|/ - uni, who fucking loves ascii art

	[EventHandler(Priority = EventPriority.Lowest)]
	public void onJoin(PlayerJoinEvent e)
	{
		Player player = e.getPlayer();
		if (string.IsNullOrEmpty(player.getName()))
		{
			player.kickPlayer();
			return;
		}
		if (isReal(player.getName()))
		{
			var plrDB = getPlrDB(player.getName());

			if (plrDB != null && plrDB.uid != player.getUniqueId()) { unauthedUsrs.Add(player.getName()); player.sendMessage($"Sorry, but {player.getName()} is already registered with a different UID. If you believe this was a mistake, please contact your server administrator."); return; } // fuck anyone who joins with a diff uid lol - uni
		}
		Console.WriteLine($"{player.getName()} joined, preparing auth");
		AuthInit(player);
	}
	[EventHandler(Priority = EventPriority.Lowest)]
	public void onPortal(PlayerPortalEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) e.setCancelled(true);
	}
	[EventHandler(Priority = EventPriority.Lowest)]
        public void onTp(PlayerTeleportEvent e)
        {
                if (unauthedUsrs.Contains(e.getPlayer().getName())) e.setCancelled(true);
        }
	[EventHandler(Priority = EventPriority.Lowest)]
        public void onInvClick(InventoryInteractEvent e)
        {
                if (unauthedUsrs.Contains(e.getWhoClicked().getName())) e.setCancelled(true);
        }
	[EventHandler(Priority = EventPriority.Lowest)]
	public void onBreak(BlockBreakEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) e.setCancelled(true);
	}
	[EventHandler(Priority = EventPriority.Lowest)]
	public void onPlace(BlockPlaceEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) e.setCancelled(true);
	}
	[EventHandler(Priority = EventPriority.Lowest)]
	public void onMove(PlayerMoveEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) e.setCancelled(true);
	}
	[EventHandler(Priority = EventPriority.Lowest)]
	public void onInventory(InventoryOpenEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) e.setCancelled(true);
	}
	[EventHandler(Priority = EventPriority.Lowest)]
	public void onDeath(PlayerDeathEvent e)
	{
		if (unauthedUsrs.Contains(e.getEntity().getName())) { // why tf is it getentity :sob: - uni
			e.setKeepLevel(true);
			e.setKeepInventory(true);
		}
	}
	[EventHandler(Priority = EventPriority.Lowest)]
	public void onChat(PlayerChatEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) {

			e.setCancelled(true);
		}
	}
	[EventHandler(Priority = EventPriority.Lowest)]
	public void onDmg(EntityDamageEvent e)
	{
		if (e.getEntity() is Player plr && unauthedUsrs.Contains(plr.getName())) e.setCancelled(true); // don't need to check if the entity is a player, only players can be unauthed :skull: - uni
	}
	[EventHandler(Priority = EventPriority.Lowest)]
	public void onDrop(PlayerDropItemEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) e.setCancelled(true);
	}
	[EventHandler(Priority = EventPriority.Lowest)]
	public void onLeave(PlayerQuitEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) unauthedUsrs.Remove(e.getPlayer().getName());
//		else {
//			var col = Database.Instance.GetCollection<PlayerDB>("playerdb");
//			var getPlr = col.Find(LiteDB.Query.EQ("Name", e.getPlayer().getName())).FirstOrDefault();
//
//			if (getPlr == null) return;
//
//			getPlr.coords = e.getPlayer().getLocation();
//
//			Console.WriteLine($"{e.getPlayer().getName()} left with internal location: {e.getPlayer().getLocation()}");
//
//			col.Update(getPlr); // i pray to god this works lol - uni
//		}
	}
}
public class Auth : CommandExecutor
{
	public bool onCommand(CommandSender sender, Command command, string label, string[] args)
	{
		if (!(sender is Player)) return false; // idk why this is needed - uni
		Player p = (Player)sender; // player initialization
		if (!AuthListener.unauthedUsrs.Contains(p.getName())) return false; // forgot a check like this - uni

		bool authworked = AuthListener.AuthFinish(p, args); // p for player, args[0] should be password? - uni

		p.sendMessage(authworked ? "Welcome!" : "Sorry, that password was incorrect.");
		return authworked;
	}
}
public class Areg : CommandExecutor // areg is the account register - uni
{
	public bool onCommand(CommandSender sender, Command command, string label, string[] args)
	{
		if (!(sender is Player)) return false; // ik why this is needed now :3 - uni
		Player p = (Player)sender;
		
		if (args.Length != 2) return false;

		if (string.IsNullOrEmpty(args[0])) return false;

		if (args[0] != args[1]) return false; // basic password confirmation - uni

		bool createworked = AuthListener.isReal(p.getName());
		if (createworked) return false;

		AuthListener.createPlr(p.getName(), args[0]);

		createworked = AuthListener.isReal(p.getName()); // reask the isReal (hehehe israel) - uni

		p.sendMessage(createworked ? $"Account '{p.getName()}' created! Do /auth <password> and remember to keep your password somewhere safe!" : "Sorry, player creation has failed. Try again");
		return createworked;
	}
}
public class AAdmin : CommandExecutor
{
	public static string passGen()
	{
    		const string allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789"; // allowed characters for passgen - uni (why did i make this comment)
    		return RandomNumberGenerator.GetString(allowedChars, 16);
	}
	
	public bool onCommand(CommandSender sender, Command command, string label, string[] args)
	{
		if (!(sender is ConsoleCommandSender)) return false; // this is server line ONLY! - uni
		
		if (!(args.Length >= 1)) return false;

		if (string.IsNullOrEmpty(args[0])) return false;

		if (args[0] == "recover")
		{
			return AuthListener.cmdRecover(args);
		}
		if (args[0] == "changepass")
		{
			return AuthListener.cmdChPass(args);
		}
		return false;
	}
}
