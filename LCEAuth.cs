namespace LCEAuth;

public class LCEAuth : ServerPlugin
{
	public void OnEnable() {
		FourKit.addListener(new AuthListener());
		FourKit.getCommand("auth").setExecutor(new Auth());
		FourKit.getCommand("areg").setExecutor(new Areg());
	}

	public void OnDisable() {}

	public string GetName() => "LCEAuth";
	public string GetVersion() => "0.2";
	public string GetAuthor() => "UniPM";
}

public class Coord
{
	public float x, y, z;
}

public class PlayerDB
{
	public string? Name { get; set; }
	public string? passCrypt { get; set; }
	public Coord? coords { get; set; }
	public string? ipAddress { get; set; }
}

public class AuthListener : Listener
{
	public static readonly string databasePath = @".\plugindb\LCEAUTH_DB_DO_NOT_MODIFY.db";

	public static bool testPass(string plrname, string pass)
	{
		if (string.IsNullOrEmpty(plrname) || string.IsNullOrEmpty(pass)) return false;
		using (var db = new LiteDB.LiteDatabase(databasePath)) // the dll executes at the exe path, so this will be a folder next to plugins named plugindb - uni
		{
			var col = db.GetCollection<PlayerDB>("playerdb");

			var getPlr = col.Find(LiteDB.Query.EQ("Name", plrname))
				.Select(x => new {GetCrypt = x.passCrypt})
				.FirstOrDefault();

			if (getPlr != null && getPlr.passCrypt != null && BCrypt.Net.BCrypt.Verify(pass, getPlr.GetCrypt)) return true; // bcrypt.net-next is really fucking weird - uni
		}
		return false;
	}

	public static bool isReal(string plrname)
	{
		if (string.IsNullOrEmpty(plrname)) return false;
		using (var db = new LiteDB.LiteDatabase(databasePath))
		{
			var col = db.GetCollection<PlayerDB>("playerdb");

			bool getPlr = col.Exists(LiteDB.Query.EQ("Name", plrname));
			return getPlr;
		}
	}

	public static void createPlr(string plrname, string pass)
	{
		if (string.IsNullOrEmpty(plrname) || string.IsNullOrEmpty(pass)) return;
		if (isReal(plrname)) return;
		using (var db = new LiteDB.LiteDatabase(databasePath))
		{
			var col = db.GetCollection<PlayerDB>("playerdb");

			var newplr = new PlayerDB
			{
				Name = plrname,
				passCrypt = BCrypt.Net.BCrypt.HashPassword(pass)
			};

			col.Insert(newplr);

			return;
		}
	}

	public static bool addressCheck(Player plr)
	{
		if (plr == null) return false;
		if (!isReal(plrname)) return false;
		using (var db = new LiteDB.LiteDatabase(databasePath))
		{
			var col = db.GetCollection<PlayerDB>("playerdb");

			var getPlr = col.Find(LiteDB.Query.EQ("Name", plr.getName()))
				.Select(x => new {GetIP = x.ipAddress})
				.FirstOrDefault();

			return (getPlr != null && getPlr.GetIP != null && BCrypt.Net.BCrypt.Verify(plr.getAddress().getAddress().getHostAddress(), getPlr.GetIP)); // so long (thats what she said) - uni
		}
	}

	public static void tpPlr(Player plr)
	{
		using (var db = new LiteDB.LiteDatabase(databasePath))
		{
			var col = db.GetCollection<PlayerDB>("playerdb");
			var coords = col.Find(LiteDB.Query.EQ("Name", plr.getName())).Select(x => new {coordins = x.coords}).FirstOrDefault().coordins;
			if (coords != null)
			{
				plr.teleport(coords.x, coords.y, coords.z);
			}
		}
	}

	public static List<string> unauthedUsrs = // god i fucking hate c# sometimes - uni
		new List<string>();
	public static void AuthInit(Player plr)
	{
		if (addressCheck(plr)) {
			plr.sendMessage($"Logged in as {plr.getName()}");
			tpPlr(plr);
			return;
		}
		using (var db = new LiteDB.LiteDatabase(databasePath))
		{
			var col = db.GetCollection<PlayerDB>("playerdb");
			var coords = col.Find(LiteDB.Query.EQ("Name", plr.getName())).Select(x => new {coordins = x.coords}).FirstOrDefault().coordins;
			if (isReal(plr.getName()) && coords != null)
			{
				plr.teleport(0f, 255f, 0f);
			}
		}
		unauthedUsrs.Add(plr.getName());
		plr.sendMessage("LCEAuth");
        plr.sendMessage("Type password to continue. /auth <password>"); // [TODO] add 30s wait - uni
		if (!isReal(plr.getName())) plr.sendMessage("No account found! Register with /areg <password> <confirmpassword>");
	}

	public static bool AuthFinish(Player plr, string pass)
	{
		if (!isReal(plr.getName())) return false;
		if (testPass(plr.getName(), pass)) {
			unauthedUsrs.Remove(plr.getName());
			using (db = new LiteDB.LiteDatabase(databasePath))
			{
				var col = db.GetCollection<PlayerDB>("playerdb");

				var getPlr = col.Find(LiteDB.Query.EQ("Name", plr.getName()))
					.FirstOrDefault();

				getPlr.ipAddress = BCrypt.Net.BCrypt.HashPassword(plr.getAddress().getAddress().getHostAddress()); // why the hell is it like this?? - uni
				// hashed for better protection :> - uni
			}
			return true;
		}
		return false;
	}

	[EventHandler]
	public void onJoin(PlayerJoinEvent e)
	{
		Player player = e.getPlayer();
		if (string.IsNullOrEmpty(player.getName()))
		{
			player.kickPlayer();
			return;
		}
		Console.WriteLine($"{player.getName()} joined, preparing auth");
		AuthInit(player);
	}
	[EventHandler]
	public void onBreak(BlockBreakEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) e.setCancelled(true);
	}
	[EventHandler]
	public void onPlace(BlockPlaceEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) e.setCancelled(true);
	}
	[EventHandler]
	public void onMove(PlayerMoveEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) e.setCancelled(true);
	}
	[EventHandler]
	public void onInventory(InventoryOpenEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) e.setCancelled(true);
	}
	[EventHandler]
	public void onInteract(PlayerInteractEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) e.setCancelled(true);
	}
	[EventHandler]
	public void onDeath(PlayerDeathEvent e)
	{
		if (unauthedUsrs.Contains(e.getEntity().getName())) { // why tf is it getentity :sob: - uni
			e.setKeepLevel(true);
			e.setKeepInventory(true);
		}
	}
	[EventHandler]
	public void onChat(PlayerChatEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) {

			e.setCancelled(true);
		}
	}
	[EventHandler]
	public void onDmg(EntityDamageEvent e)
	{
		if (e.getEntity() is Player plr && unauthedUsrs.Contains(plr.getName())) e.setCancelled(true); // don't need to check if the entity is a player, only players can be unauthed :skull: - uni
	}
	[EventHandler]
	public void onDrop(PlayerDropItemEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) e.setCancelled(true);
	}
	[EventHandler]
	public void onLeave(PlayerLeaveEvent e)
	{
		if (unauthedUsrs.Contains(e.getPlayer().getName())) unauthedUsrs.Remove(e.getPlayer().getName());
		else {
			using (var db = new LiteDB.LiteDatabase(databasePath))
			{
				var col = db.GetCollection<PlayerDB>("playerdb");
				var getPlr = col.Find(LiteDB.Query.EQ("Name", plr.getName())).FirstOrDefault();

				getPlr.coords = new Coord
				{
					x = e.getPlayer().getX(),
					y = e.getPlayer().getY(),
					z = e.getPlayer().getZ()
				};

				col.Update(getPlr); // i pray to god this works lol - uni
			}
		}
	}
}
public class Auth : CommandExecutor
{
	public bool onCommand(CommandSender sender, Command command, string label, string[] args)
	{
		if (!(sender is Player)) return false; // idk why this is needed - uni
		Player p = (Player)sender; // player initialization

		bool authworked = AuthListener.AuthFinish(p, args[0]); // p for player, args[0] should be password? - uni

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
