namespace LCEAuth;

public class LCEAuth : ServerPlugin
{
	public void OnEnable() {
		FourKit.addListener(new AuthListener());
		FourKit.getCommand("auth").setExecutor(new Auth());
	}

	public void OnDisable() {}

	public string GetName() => "LCEAuth";
	public string GetVersion() => "0.1";
	public string GetAuthor() => "UniPM";
}

public class AuthListener : Listener
{
	public static List<string> unauthedUsrs = // god i fucking hate c# sometimes - uni
		new List<string>();
	public static void AuthInit(Player plr)
	{
		unauthedUsrs.Add(plr.getName());
		plr.sendMessage("LCEAuth 0.1");
                plr.sendMessage("Type password to continue. /auth password"); // [TODO] add 30s wait - uni
		Console.WriteLine($"UnauthedUsrs is: {unauthedUsrs}");
	}

	public static bool AuthFinish(Player plr, string pass)
	{
		if (pass == "password") {unauthedUsrs.Remove(plr.getName()); return true;}
		return false;
	}

	[EventHandler]
	public void onJoin(PlayerJoinEvent e)
	{
		Player player = e.getPlayer();
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
		if (unauthedUsrs.Contains(e.getPlayer().getName())) e.setCancelled(true);
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
