using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
using SharpDX;

namespace KurisuRiven
{
    internal class KurisuRiven
    {
        #region Riven: Main

        private static int lastq;
        private static int lastw;
        private static int laste;
        private static int lastaa;
        private static int lasthd;
        private static int lastwd;

        private static bool canq;
        private static bool canw;
        private static bool cane;
        private static bool canmv;
        private static bool canaa;
        private static bool canws;
        private static bool canhd;
        private static bool hashd;

        private static bool didq;
        private static bool didw;
        private static bool dide;
        private static bool didws;
        private static bool didaa;
        private static bool didhd;
        private static bool didhs;
        private static bool ssfl;

        private static Menu menu;
        private static Spell q, w, e, r;
        private static Orbwalking.Orbwalker orbwalker;
        private static Obj_AI_Hero player = ObjectManager.Player;
        private static HpBarIndicator hpi = new HpBarIndicator();
        private static Obj_AI_Base qtarg; // semi q target

        private static int qq;
        private static int cc;
        private static int pc;  
        private static bool uo;
        private static SpellSlot flash;

        private static float wrange;
        private static float truerange;
        private static Vector3 movepos;

        #endregion

        # region Riven: Utils

        private static bool menubool(string item)
        {
            return menu.Item(item).GetValue<bool>();
        }

        private static int menuslide(string item)
        {
            return menu.Item(item).GetValue<Slider>().Value;
        }

        private static int menulist(string item)
        {
            return menu.Item(item).GetValue<StringList>().SelectedIndex;
        }

        private static float xtra(float dmg)
        {
           return r.IsReady() ? (float) (dmg + (dmg*0.2)) : dmg;
        }

        private static bool IsLethal(Obj_AI_Base unit)
        {
            return ComboDamage(unit) / 1.65 >= unit.Health;
        }

        private static Obj_AI_Base GetCenterMinion()
        {
            var minionposition = MinionManager.GetMinions(300 + q.Range).Select(x => x.Position.To2D()).ToList();
            var center = MinionManager.GetBestCircularFarmLocation(minionposition, 250, 300 + q.Range);

            return center.MinionsHit >= 3
                ? MinionManager.GetMinions(1000).OrderBy(x => x.Distance(center.Position)).FirstOrDefault()
                : null;
        }

        private static void TryIgnote(Obj_AI_Base target)
        {
            var ignote = player.GetSpellSlot("summonerdot");
            if (player.Spellbook.CanUseSpell(ignote) == SpellState.Ready)
            {
                if (target.Distance(player.ServerPosition) <= 600)
                {
                    if (cc <= menuslide("userq") && q.IsReady() && menubool("useignote"))
                    {
                        if (ComboDamage(target) >= target.Health &&
                            target.Health / target.MaxHealth * 100 > menuslide("overk"))
                        {
                            if (r.IsReady() && uo)
                            {
                                player.Spellbook.CastSpell(ignote, target);
                            }
                        }
                    }
                }
            }
        }

        private static void useinventoryitems(Obj_AI_Base target)
        {
            if (Items.HasItem(3142) && Items.CanUseItem(3142))
                Items.UseItem(3142);

            if (target.Distance(player.ServerPosition, true) <= 450 * 450)
            {
                if (Items.HasItem(3144) && Items.CanUseItem(3144))
                    Items.UseItem(3144, target);
                if (Items.HasItem(3153) && Items.CanUseItem(3153))
                    Items.UseItem(3153, target);
            }
        }

        private static readonly string[] minionlist =
        {
            // summoners rift
            "SRU_Razorbeak", "SRU_Krug", "Sru_Crab", "SRU_Baron", "SRU_Dragon",
            "SRU_Blue", "SRU_Red", "SRU_Murkwolf", "SRU_Gromp", 
            
            // twisted treeline
            "TT_NGolem5", "TT_NGolem2", "TT_NWolf6", "TT_NWolf3",
            "TT_NWraith1", "TT_Spider"
        };

        #endregion

        public KurisuRiven()
        {
            CustomEvents.Game.OnGameLoad += args =>
            {
                try
                {
                    if (player.ChampionName != "Riven") 
                        return;

                    w = new Spell(SpellSlot.W, 250f);
                    e = new Spell(SpellSlot.E, 270f);

                    q = new Spell(SpellSlot.Q, 260f);
                    q.SetSkillshot(0.25f, 100f, 2200f, false, SkillshotType.SkillshotCircle);

                    r = new Spell(SpellSlot.R, 900f);
                    r.SetSkillshot(0.25f, (float) (45 * 0.5), 1600f, false, SkillshotType.SkillshotCircle);

                    flash = player.GetSpellSlot("summonerflash");

                    OnNewPath();
                    OnPlayAnimation();
                    Interrupter();
                    OnGapcloser();
                    OnCast();
                    Drawings();
                    OnMenuLoad();

                    Game.OnUpdate += Game_OnUpdate;
                    Game.OnWndProc += Game_OnWndProc;

                    Game.PrintChat("<b>Kurisu's Riven</b> - Loaded!");
                    Updater.UpdateCheck();

                    if (menu.Item("Farm").GetValue<KeyBind>().Key == menu.Item("semiq").GetValue<KeyBind>().Key ||
                        menu.Item("Orbwalk").GetValue<KeyBind>().Key == menu.Item("semiq").GetValue<KeyBind>().Key ||
                        menu.Item("LaneClear").GetValue<KeyBind>().Key == menu.Item("semiq").GetValue<KeyBind>().Key ||
                        menu.Item("LastHit").GetValue<KeyBind>().Key == menu.Item("semiq").GetValue<KeyBind>().Key)
                    {
                        Console.WriteLine(
                            "<b><font color=\"#FF9900\">" +
                            "WARNING: Semi-Q Keybind Should not be the same key as any of " +
                            "the other orbwalking modes or it will not Work!</font></b>");
                    }

                }

                catch (Exception e)
                {
                    Console.WriteLine("Fatal Error: " + e.Message);
                }
            };
        }

        private static Obj_AI_Hero _sh;
        void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == (ulong) WindowsMessages.WM_LBUTTONDOWN)
            {
                _sh = HeroManager.Enemies
                     .FindAll(hero => hero.IsValidTarget() && hero.Distance(Game.CursorPos, true) < 40000) // 200 * 200
                     .OrderBy(h => h.Distance(Game.CursorPos, true)).FirstOrDefault();
            }
        }


        private static Obj_AI_Hero riventarget()
        {
            var cursortarg = HeroManager.Enemies
                .Where(x => x.Distance(Game.CursorPos) <= 375 &&  x.Distance(player.ServerPosition) <= 1200)
                .OrderBy(x => x.Distance(Game.CursorPos)).FirstOrDefault(x => x.IsEnemy);

            var closetarg = HeroManager.Enemies
                .Where(x => x.Distance(player.ServerPosition) <= 1200)
                .OrderBy(x => x.Distance(player.ServerPosition)).FirstOrDefault(x => x.IsEnemy);

            return _sh ?? cursortarg ?? closetarg;
        }

        #region Riven: OnNewPath 
        private static void OnNewPath()
        {
            Obj_AI_Base.OnNewPath += (sender, args) =>
            {
                if (sender.IsMe && !args.IsDash)
                {
                    if (args.Path.Count() > 1)
                    {
                        canmv = true;
                        canaa = true;
                    }

                    if (didq)
                    {
                        didq = false;
                        canmv = true;
                        canaa = true;
                    }
                }
            };
        }

        #endregion

        #region Riven: OnUpdate
        private static void Game_OnUpdate(EventArgs args)
        {
            // harass active
            didhs = menu.Item("harasskey").GetValue<KeyBind>().Active;

            // ulti check
            uo = player.GetSpell(SpellSlot.R).Name != "RivenFengShuiEngine";

            // hydra check
            hashd = Items.HasItem(3077) || Items.HasItem(3074) || Items.HasItem(3748);
            canhd = canmv && (Items.CanUseItem(3077) || Items.CanUseItem(3074) || Items.CanUseItem(3748));

            // my radius
            truerange = player.AttackRange + player.Distance(player.BBox.Minimum) + 1;

            // if no valid target cancel to cursor pos
            if (!qtarg.IsValidTarget(truerange + 100))
                 qtarg = player;

            if (_sh.IsValidTarget())
            {
                if (menu.Item("combokey").GetValue<KeyBind>().Active ||
                    menu.Item("harasskey").GetValue<KeyBind>().Active ||
                    menu.Item("shycombo").GetValue<KeyBind>().Active)
                {
                    orbwalker.ForceTarget(_sh);
                }
            }

            else
                _sh = null;

            // riven w range
            wrange = uo ? w.Range + 25 : w.Range;

            switch (menulist("qcancel"))
            {
                case 0:
                    // move behind me
                    if (qtarg != player && qtarg.IsFacing(player) && qtarg.Distance(player.ServerPosition) < truerange + 120)
                        movepos = player.ServerPosition + (player.ServerPosition - qtarg.ServerPosition).Normalized() * 24;

                    // move towards target (thanks yol0)
                    if (qtarg != player && (!qtarg.IsFacing(player) || qtarg.Distance(player.ServerPosition) > truerange + 120))
                        movepos = player.ServerPosition.Extend(qtarg.ServerPosition, 350);

                    // move to game cursor pos
                    if (qtarg == player)
                        movepos = player.ServerPosition + (Game.CursorPos - player.ServerPosition).Normalized() * 125;
                    break;
                case 1:
                    // move behind me
                    if (qtarg != player && qtarg.Distance(player.ServerPosition) <= 500)
                        movepos = player.ServerPosition + (player.ServerPosition - qtarg.ServerPosition).Normalized() * 24;

                    // move to game cursor pos
                    if (qtarg == player)
                        movepos = player.ServerPosition + (Game.CursorPos - player.ServerPosition).Normalized() * 125;

                    break;
                case 2:
                    // move towards target (thanks yol0)
                    if (qtarg != player && qtarg.Distance(player.ServerPosition) <= 500)
                        movepos = player.ServerPosition.Extend(qtarg.ServerPosition, 350);

                    // move to game cursor pos
                    if (qtarg == player)
                        movepos = player.ServerPosition + (Game.CursorPos - player.ServerPosition).Normalized() * 95;
                    break;
                case 3:
                    // move to game cursor pos
                    movepos = player.ServerPosition + (Game.CursorPos - player.ServerPosition).Normalized() * 125;
                    break;
            }

            SemiQ();
            AuraUpdate();
            CombatCore();
            Windslash();

            orbwalker.SetAttack(canmv);
            orbwalker.SetMovement(canmv);

            if (riventarget().IsValidTarget() && 
                menu.Item("combokey").GetValue<KeyBind>().Active)
            {
                ComboTarget(riventarget());
                TryIgnote(riventarget());
            }

            if (menu.Item("shycombo").GetValue<KeyBind>().Active)
            {
                OrbTo(riventarget(), 350);

                if (!riventarget().IsValidTarget())
                    return;

                if (riventarget().Distance(player.ServerPosition) <= wrange)
                    w.Cast();

                SomeDash(riventarget());
                TryIgnote(riventarget());

                if (q.IsReady() && riventarget().Distance(player.ServerPosition) <= truerange + 100)
                {
                    useinventoryitems(riventarget());
                    checkr();

                    if (canhd)
                    {
                        return;
                    }

                    if (canq)
                    {
                        if (Utils.GameTimeTickCount - lastw >= 350)
                        {
                            q.Cast(riventarget().ServerPosition);
                        }
                    }
                }

            }

            if (didhs && riventarget().IsValidTarget())
                HarassTarget(riventarget());

            if (player.IsValid && menu.Item("clearkey").GetValue<KeyBind>().Active)
            {
                Clear();
                Wave();
            }

            if (player.IsValid && menu.Item("fleekey").GetValue<KeyBind>().Active)
                Flee();
        }

        #endregion

        #region Riven: Menu
        private static void OnMenuLoad()
        {
            menu = new Menu("Kurisu's Riven", "kurisuriven", true);

            var orbwalkah = new Menu("Orbwalk", "rorb");
            orbwalker = new Orbwalking.Orbwalker(orbwalkah);
            menu.AddSubMenu(orbwalkah);

            var keybinds = new Menu("Keybinds", "keybinds");
            keybinds.AddItem(new MenuItem("combokey", "Combo")).SetValue(new KeyBind(32, KeyBindType.Press));
            keybinds.AddItem(new MenuItem("harasskey", "Harass")).SetValue(new KeyBind(67, KeyBindType.Press));
            keybinds.AddItem(new MenuItem("clearkey", "Jungle/Laneclear")).SetValue(new KeyBind(86, KeyBindType.Press));
            keybinds.AddItem(new MenuItem("fleekey", "Flee")).SetValue(new KeyBind(65, KeyBindType.Press));
            keybinds.AddItem(new MenuItem("shycombo", "Shy Burst")).SetValue(new KeyBind('T', KeyBindType.Press));

            var mitem = new MenuItem("semiqlane", "Use Semi-Q Laneclear");
            mitem.ValueChanged += (sender, args) =>
            {
                if (menu.Item("Farm").GetValue<KeyBind>().Key == args.GetNewValue<KeyBind>().Key ||
                    menu.Item("Orbwalk").GetValue<KeyBind>().Key == args.GetNewValue<KeyBind>().Key ||
                    menu.Item("LaneClear").GetValue<KeyBind>().Key == args.GetNewValue<KeyBind>().Key ||
                    menu.Item("LastHit").GetValue<KeyBind>().Key == args.GetNewValue<KeyBind>().Key)
                {
                    Game.PrintChat(
                        "<b><font color=\"#FF9900\">" +
                        "WARNING: Semi-Q Keybind Should not be the same key as any of " +
                        "the other orbwalking modes or it will not Work!</font></b>");
                }
            };

            keybinds.AddItem(mitem).SetValue(new KeyBind(71, KeyBindType.Press));
            keybinds.AddItem(new MenuItem("semiq", "Use Semi-Q Harass/Jungle")).SetValue(true);
            menu.AddSubMenu(keybinds);

            var drMenu = new Menu("Drawings", "drawings");
            drMenu.AddItem(new MenuItem("linewidth", "Line Width")).SetValue(new Slider(1, 1, 6));
            drMenu.AddItem(new MenuItem("drawengage", "Draw Engage Range")).SetValue(new Circle(true, Color.FromArgb(150, Color.Gold)));
            drMenu.AddItem(new MenuItem("drawr2", "Draw R2 Range")).SetValue(new Circle(true, Color.FromArgb(150, Color.Gold)));
            drMenu.AddItem(new MenuItem("drawburst", "Draw Burst Range")).SetValue(new Circle(true, Color.FromArgb(150, Color.LawnGreen)));
            drMenu.AddItem(new MenuItem("drawf", "Draw Focused Target")).SetValue(new Circle(true, Color.FromArgb(255, Color.Red)));
            drMenu.AddItem(new MenuItem("drawdmg", "Draw Combo Damage Fill")).SetValue(true);
            menu.AddSubMenu(drMenu);

            var combo = new Menu("Combo", "combo");
            var qmenu = new Menu("Q  Settings", "rivenq");
            var advance = new Menu("Q Advance Settings", "advance");
            advance.AddItem(new MenuItem("qcancel", "Cancel Direction: "))
                .SetValue(new StringList(new[] {"Auto", "Behind Me", "Target", "Cursor"}, 1));
            advance.AddItem(new MenuItem("autoaq", "Can Q Delay (ms)")).SetValue(new Slider(25, -100, 300));
            advance.AddItem(new MenuItem("qqc", "Test in a summoners rift custom on the Scuttler Crab")).SetFontStyle(FontStyle.Regular, SharpDX.Color.Gold);
            advance.AddItem(new MenuItem("qqa", "Lower = faster Q but may result in more AA cancels"));
            advance.AddItem(new MenuItem("qqb", "Higher = slower Q but less or no AA cancels"));
            advance.AddItem(new MenuItem("qqd", "Lower your orbwalker radius for a better Q-AA")).SetFontStyle(FontStyle.Regular, SharpDX.Color.Gold);
            qmenu.AddSubMenu(advance);

            qmenu.AddItem(new MenuItem("wq3", "Ward + Q3 (Flee)")).SetValue(true);
            qmenu.AddItem(new MenuItem("qint", "Interrupt with 3rd Q")).SetValue(true);
            qmenu.AddItem(new MenuItem("keepq", "Keep Q Buff Up")).SetValue(true);
            qmenu.AddItem(new MenuItem("usegap", "Gapclose with Q")).SetValue(true);
            qmenu.AddItem(new MenuItem("gaptimez", "Gapclose Q Delay (ms)")).SetValue(new Slider(115, 0, 200));
            combo.AddSubMenu(qmenu);

            var wmenu = new Menu("W Settings", "rivenw");
            wmenu.AddItem(new MenuItem("usecombow", "Use W in Combo")).SetValue(true);
            wmenu.AddItem(new MenuItem("wmode", "Use W Mode"))
                .SetValue(new StringList(new[] {"W -> AA -> Q", "W -> Q -> AA"}, 1));
            wmenu.AddItem(new MenuItem("wgap", "Use W on Gapcloser")).SetValue(true);
            wmenu.AddItem(new MenuItem("wint", "Use W to Interrupt")).SetValue(true);
            combo.AddSubMenu(wmenu);

            var emenu = new Menu("E  Settings", "rivene");
            emenu.AddItem(new MenuItem("usecomboe", "Use E in Combo")).SetValue(true);
            emenu.AddItem(new MenuItem("emode", "Use E Mode"))
                .SetValue(new StringList(new[] { "E -> W/R -> Tiamat -> Q", "E -> Tiamat -> W/R -> Q" }));
            emenu.AddItem(new MenuItem("vhealth", "Use E if HP% <=")).SetValue(new Slider(40));
            emenu.AddItem(new MenuItem("ashield", "Shield Targeted Spells While LastHit")).SetValue(false);
            emenu.AddItem(new MenuItem("bshield", "Shield Self/AoE Spells While LastHit")).SetValue(false);
            combo.AddSubMenu(emenu);

            var rmenu = new Menu("R  Settings", "rivenr");
            rmenu.AddItem(new MenuItem("user", "Use R1 in Combo")).SetValue(new KeyBind('H', KeyBindType.Toggle, true)).Permashow();
            rmenu.AddItem(new MenuItem("useignote", "Use R + Ignite")).SetValue(true);
            rmenu.AddItem(new MenuItem("multib", "Flash -> R/W When")).SetValue(new StringList(new [] { "Can Burst Target", "Always", "Dont Flash"}));
            rmenu.AddItem(new MenuItem("overk", "Dont R if Target HP % <=")).SetValue(new Slider(25, 1, 99));
            rmenu.AddItem(new MenuItem("userq", "Use R Only if Q Count <=")).SetValue(new Slider(2, 1, 3));
            rmenu.AddItem(new MenuItem("ultwhen", "Use R When")).SetValue(new StringList(new[] {"Normal Kill", "Hard Kill", "Always"}, 2));
            rmenu.AddItem(new MenuItem("usews", "Use R2 in Combo")).SetValue(true);
            rmenu.AddItem(new MenuItem("wsmode", "Use R2 When")).SetValue(new StringList(new[] {"Kill Only", "Kill Or MaxDamage"}, 1));
            combo.AddSubMenu(rmenu);

            menu.AddSubMenu(combo);

            var harass = new Menu("Harass", "harass");
            harass.AddItem(new MenuItem("qtoo", "Use 3rd Q:"))
                .SetValue(new StringList(new[] {"Away from Target", "To Ally Turret", "To Cursor"}, 1));
            harass.AddItem(new MenuItem("useharassw", "Use W in Harass")).SetValue(true);
            harass.AddItem(new MenuItem("usegaph", "Use E in Harass (Gapclose)")).SetValue(true);
            harass.AddItem(new MenuItem("useitemh", "Use Tiamat/Hydra")).SetValue(true);
            menu.AddSubMenu(harass);

            var farming = new Menu("Farming", "farming");

            var wc = new Menu("Jungle", "waveclear");
            wc.AddItem(new MenuItem("usejungleq", "Use Q in Jungle")).SetValue(true);
            wc.AddItem(new MenuItem("usejunglew", "Use W in Jungle")).SetValue(true);
            wc.AddItem(new MenuItem("usejunglee", "Use E in Jungle")).SetValue(true);
            farming.AddSubMenu(wc);

            var jg = new Menu("WaveClear", "jungle");
            jg.AddItem(new MenuItem("uselaneq", "Use Q in WaveClear")).SetValue(true);
            jg.AddItem(new MenuItem("uselanew", "Use W in WaveClear")).SetValue(true);
            jg.AddItem(new MenuItem("wminion", "Use W Minions >=")).SetValue(new Slider(3, 1, 6));
            jg.AddItem(new MenuItem("uselanee", "Use E in WaveClear")).SetValue(true);
            farming.AddSubMenu(jg);

            menu.AddSubMenu(farming);
            menu.AddToMainMenu();

        }

        #endregion

        #region Riven : Some Dash
        private static bool canburst(bool shy = false)
        {
            if (riventarget() == null || !r.IsReady() && !uo)
            {
                return false;
            }

            if (IsLethal(riventarget()) && menulist("multib") == 0)
            {
                return true;
            }

            if (shy && menulist("multib") != 0)
            {
                return true;
            }
            
            return false;
        }

        private static void SomeDash(Obj_AI_Hero target)
        {
            if (!menu.Item("shycombo").GetValue<KeyBind>().Active ||
                !target.IsValid<Obj_AI_Hero>() || uo)
                return;

            if (riventarget() == null || uo || !r.IsReady())
                return;

            if (flash.IsReady() && canburst(true) && menulist("multib") != 2)
            {
                if (e.IsReady() && target.Distance(player.ServerPosition) <= e.Range + 50 + 300)
                {
                    if (target.Distance(player.ServerPosition) > e.Range + truerange)
                    {
                        e.Cast(target.ServerPosition);
                        r.Cast();
                    }
                }

                if (!e.IsReady() && target.Distance(player.ServerPosition) <= 50 + 300)
                {
                    if (target.Distance(player.ServerPosition) > truerange + 35)
                    {
                        r.Cast();
                    }
                }
            }

            else
            {
                if (e.IsReady() && target.Distance(player.ServerPosition) <= e.Range + w.Range)
                {
                    e.Cast(target.ServerPosition);
                    r.Cast();
                }
            }
        }

        #endregion

        #region Riven: Combo

        private static void ComboTarget(Obj_AI_Base target)
        {
            // orbwalk ->
            OrbTo(target);

            // ignite ->
            TryIgnote(target);

            if (e.IsReady() && cane && player.Health / player.MaxHealth * 100 <= menuslide("vhealth") && target.Distance(player.ServerPosition) <=
                e.Range + w.Range - 25 || e.IsReady() && cane && target.Distance(player.ServerPosition) <= 
                e.Range + w.Range - 25 && target.Distance(player.ServerPosition) > truerange ||
                e.IsReady() && uo && cane && target.Distance(player.ServerPosition) > truerange + 50)
            {
                if (menubool("usecomboe"))
                    e.Cast(target.ServerPosition);

                if (target.Distance(player.ServerPosition) <= e.Range + w.Range + 25)
                {
                    if (menulist("emode") == 1)
                    {
                        if (canhd && hashd && !canburst() && cc < 2)
                        {
                            Items.UseItem(3077);
                            Items.UseItem(3074);
                        }

                        else
                        {
                            checkr();
                        }
                    }

                    if (menulist("emode") == 0)
                    {
                        checkr();
                    }
                }
            }

            else if (w.IsReady() && canw && menubool("usecombow") &&
                     target.Distance(player.ServerPosition) <= wrange)
            {
                useinventoryitems(target);
                checkr();

                if (menulist("emode") == 1)
                {
                    if (canhd && hashd && !canburst())
                    {
                        Items.UseItem(3077);
                        Items.UseItem(3074);
                        if (menubool("usecombow"))
                            Utility.DelayAction.Add(250, () => w.Cast());
                    }

                    else
                    {
                        checkr();
                        if (menubool("usecombow"))
                            w.Cast();
                    }
                }

                if (menulist("emode") == 0)
                {
                    if (menubool("usecombow"))
                        w.Cast();
                }
            }

            else if (q.IsReady() && target.Distance(player.ServerPosition) <= truerange + 150)
            {
                useinventoryitems(target);
                checkr();

                if (menulist("emode") == 0 || IsLethal(target))
                {
                    // wait for aa -> tiamat
                    if (canhd) return;
                }

                if (menulist("wsmode") == 1 && IsLethal(target))
                {
                    if (cc == 2 && e.IsReady() && cane)
                    {
                        e.Cast(target.ServerPosition);
                    }
                }

                if (canq) q.Cast(target.ServerPosition);
            }

            else if (target.Distance(player.ServerPosition) > truerange + 100 && !e.IsReady())
            {
                if (menubool("usegap") && (!r.IsReady() || uo))
                {
                    if (Utils.GameTimeTickCount - lastq >= menuslide("gaptimez") * 10)
                    {
                        if (q.IsReady() && Utils.GameTimeTickCount - laste >= 600)
                        {
                            q.Cast(target.ServerPosition);
                        }
                    }
                }
            }
        }

        #endregion

        #region Riven: Harass

        private static void HarassTarget(Obj_AI_Base target)
        {
            Vector3 qpos;
            switch (menulist("qtoo"))
            {
                case 0:
                    qpos = player.ServerPosition + 
                        (player.ServerPosition - target.ServerPosition).Normalized()*500;
                    break;
                case 1:
                    qpos = ObjectManager.Get<Obj_AI_Turret>()
                        .Where(t => (t.IsAlly)).OrderBy(t => t.Distance(player.Position)).First().Position;
                    break;
                default:
                    qpos = Game.CursorPos;
                    break;
            }

            if (q.IsReady())
                OrbTo(target);

            if (cc == 2 && canq && q.IsReady())
            {
                orbwalker.SetAttack(false);
                orbwalker.SetAttack(false);

                canaa = false;
                canmv = false;

                player.IssueOrder(GameObjectOrder.MoveTo, qpos);
                Utility.DelayAction.Add(200, () =>
                {
                    q.Cast(qpos);
                    orbwalker.SetAttack(true);
                    orbwalker.SetAttack(true);
                });
            }

            if (!player.ServerPosition.Extend(target.ServerPosition, q.Range*3).UnderTurret(true))
            {
                if (q.IsReady() && canq && cc < 2)
                {
                    if (target.Distance(player.ServerPosition) <= truerange + q.Range)
                    {
                        q.Cast(target.ServerPosition);
                    }
                }
            }

            if (e.IsReady() && cane && q.IsReady() && cc < 1 &&
                target.Distance(player.ServerPosition) > truerange + 100 &&
                target.Distance(player.ServerPosition) <= e.Range + truerange + 50)
            {
                if (!player.ServerPosition.Extend(target.ServerPosition, e.Range).UnderTurret(true))
                {
                    if (menubool("usegaph"))
                    {
                        e.Cast(target.ServerPosition);

                        if (canhd)
                        {
                            if (Items.CanUseItem(3077))
                                Items.UseItem(3077);
                            if (Items.CanUseItem(3074))
                                Items.UseItem(3074);
                        }
                    }
                }
            }

            else if (w.IsReady() && canw && target.Distance(player.ServerPosition) <= w.Range + 10)
            {
                if (!target.ServerPosition.UnderTurret(true))
                {
                    if (menubool("useharassw"))
                    {
                        w.Cast();
                    }
                }
            }

        }

        #endregion
         
        #region Riven: Windslash

        private static void Windslash()
        {
            if (uo && menubool("usews") && r.IsReady())
            {
                foreach (var t in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsValidTarget(r.Range)))
                {
                    if (menu.Item("shycombo").GetValue<KeyBind>().Active && canburst())
                    {
                        if (t.Distance(player.ServerPosition) <= player.AttackRange + 100)
                        {
                            if (canhd) return;
                        }
                    }

                    // only kill or killsteal etc ->
                    if (r.GetDamage(t) >= t.Health && canws)
                    {
                        if (r.GetPrediction(t, true).Hitchance == HitChance.VeryHigh)
                            r.Cast(r.GetPrediction(t, true).CastPosition);
                    }
                }

                if (menulist("wsmode") == 1)
                {
                    if (riventarget().IsValidTarget(r.Range) && !riventarget().IsZombie)
                    {
                        if (menu.Item("shycombo").GetValue<KeyBind>().Active && canburst())
                        {
                            if (riventarget().Distance(player.ServerPosition) <= player.AttackRange + 100)
                            {
                                if (canhd) return;
                            }
                        }

                        var cx = 4 - cc;
                        if (r.GetDamage(riventarget()) / riventarget().MaxHealth * 100 >= 50)
                        {
                            if (r.GetPrediction(riventarget(), true).Hitchance >= HitChance.Medium && canws)
                                r.Cast(r.GetPrediction(riventarget(), true).CastPosition);
                        }

                        if (q.IsReady())
                        {
                            // teh bro logic
                            var cy = r.GetDamage(riventarget()) + 
                                     player.GetAutoAttackDamage(riventarget()) * 2 + Qdmg(riventarget()) * cx;

                            if (riventarget().Health <= xtra((float) cy))
                            {
                                if (riventarget().Distance(player.ServerPosition) <= truerange + q.Range * cx)
                                {
                                    if (r.GetPrediction(riventarget(), true).Hitchance >= HitChance.High && canws)
                                        r.Cast(r.GetPrediction(riventarget(), true).CastPosition);
                                }
                            }
                        }
                    }
                }
            }        
        }

        #endregion

        #region Riven: Lane/Jungle

        private static void Clear()
        {
            var minions = MinionManager.GetMinions(player.Position, 600f,
                MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            foreach (var unit in minions.Where(m => !m.Name.Contains("Mini")))
            {
                OrbTo(unit);

                if (q.IsReady() && canq && menubool("usejungleq"))
                {
                    if (unit.Distance(player.ServerPosition) <= q.Range + 100)
                    {
                        if (menulist("emode") == 0)
                        {
                            if (canhd) return;
                        }

                        q.Cast(unit.ServerPosition);
                    }
                }

                if (w.IsReady() && canw && menubool("usejunglew"))
                {
                    if (unit.Distance(player.ServerPosition) <= w.Range + 10)
                    {
                        w.Cast();
                    }
                }

                if (e.IsReady() && cane && menubool("usejunglee"))
                {
                    if (player.Health / player.MaxHealth * 100 <= 70 ||
                        unit.Distance(player.ServerPosition) > truerange + 30)
                    {
                        e.Cast(unit.ServerPosition);
                    }
                }
            }
        }

        private static void Wave()
        {
            var minions = MinionManager.GetMinions(player.Position, 600f);

            foreach (var unit in minions.Where(x => x.IsMinion))
            {
                if (player.GetAutoAttackDamage(unit, true) >= unit.Health)
                    OrbTo(GetCenterMinion().IsValidTarget() ? GetCenterMinion() : unit);

                if (q.IsReady() && unit.Distance(player.ServerPosition) <= truerange + 100)
                {
                    if (canq && menubool("uselaneq") && minions.Count >= 2 &&
                        !player.ServerPosition.Extend(unit.ServerPosition, q.Range).UnderTurret(true))
                    {
                        if (GetCenterMinion().IsValidTarget())
                            q.Cast(GetCenterMinion());
                        else
                            q.Cast(unit.ServerPosition);
                    }
                }

                if (w.IsReady())
                {
                    if (minions.Count(m => m.Distance(player.ServerPosition) <= w.Range + 10) >= menuslide("wminion"))
                    {
                        if (canw && menubool("uselanew"))
                        {
                            Items.UseItem(3077);
                            Items.UseItem(3074);
                            Items.UseItem(3748);
                            w.Cast();
                        }
                    }
                }

                if (e.IsReady() && !player.ServerPosition.Extend(unit.ServerPosition, e.Range).UnderTurret(true))
                {
                    if (unit.Distance(player.ServerPosition) > truerange + 30)
                    {
                        if (cane && menubool("uselanee"))
                        {
                            if (GetCenterMinion().IsValidTarget())
                                e.Cast(GetCenterMinion());
                            else
                                e.Cast(unit.ServerPosition);
                        }
                    }

                    else if (player.Health / player.MaxHealth * 100 <= 70)
                    {
                        if (cane && menubool("uselanee"))
                        {
                            if (GetCenterMinion().IsValidTarget())
                                q.Cast(GetCenterMinion());
                            else
                                q.Cast(unit.ServerPosition);
                        }
                    }
                }
            }
        }

        #endregion

        #region Riven: Flee

        private static void Flee()
        {
            if (canmv)
            {
                player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }

            if (cc > 2 && didq && Items.GetWardSlot() != null && menubool("wq3"))
            {
                var attacker = HeroManager.Enemies.FirstOrDefault(x => x.Distance(player.ServerPosition) <= q.Range);
                if (attacker != null && !player.IsFacing(attacker))
                {
                    if (Utils.GameTimeTickCount - lastwd >= 1000)
                    {
                        Utility.DelayAction.Add(100,
                            () => Items.UseItem((int) Items.GetWardSlot().Id, attacker.ServerPosition));
                    }
                }
            }

            if (player.CountEnemiesInRange(w.Range) > 0)
            {
                if (w.IsReady())
                    w.Cast();
            }

            if (ssfl)
            {
                if (Utils.GameTimeTickCount - lastq >= 600)
                {
                    q.Cast(Game.CursorPos);
                }

                if (cane && e.IsReady())
                {
                    if (cc >= 2 || !q.IsReady() && !player.HasBuff("RivenTriCleave", true))
                    {
                        if (!player.ServerPosition.Extend(Game.CursorPos, e.Range + 10).IsWall())
                            e.Cast(Game.CursorPos);
                    }
                }
            }

            else
            {
                if (q.IsReady())
                {
                    q.Cast(Game.CursorPos);
                }

                if (e.IsReady() && Utils.GameTimeTickCount - lastq >= 250)
                {
                    if (!player.ServerPosition.Extend(Game.CursorPos, e.Range).IsWall())
                        e.Cast(Game.CursorPos);
                }
            }
        }

        #endregion

        #region Riven: SemiQ 

        private static void SemiQ()
        {
            if (canq && Utils.GameTimeTickCount - lastaa >= 150 && menubool("semiq"))
            {
                if (q.IsReady() && Utils.GameTimeTickCount - lastaa < 1200 && qtarg != null)
                {
                    if (qtarg.IsValidTarget(q.Range + 100) &&
                       !menu.Item("clearkey").GetValue<KeyBind>().Active &&
                       !menu.Item("harasskey").GetValue<KeyBind>().Active &&
                       !menu.Item("combokey").GetValue<KeyBind>().Active &&
                       !menu.Item("shycombo").GetValue<KeyBind>().Active)
                    {
                        if (qtarg.IsValid<Obj_AI_Hero>())
                            q.Cast(qtarg.ServerPosition);
                    }

                    if (!menu.Item("harasskey").GetValue<KeyBind>().Active &&
                        !menu.Item("clearkey").GetValue<KeyBind>().Active &&
                        !menu.Item("combokey").GetValue<KeyBind>().Active &&
                        !menu.Item("shycombo").GetValue<KeyBind>().Active)
                    {
                        if (qtarg.IsValidTarget(q.Range + 100) && !qtarg.Name.Contains("Mini"))
                        {
                            if (!qtarg.Name.StartsWith("Minion") && minionlist.Any(name => qtarg.Name.StartsWith(name)))
                            {
                                q.Cast(qtarg.ServerPosition);
                            }
                        }

                        if (qtarg.IsValidTarget(q.Range + 100))
                        {
                            if (qtarg.IsValid<Obj_AI_Minion>() || qtarg.IsValid<Obj_AI_Turret>())
                            {
                                if (menu.Item("semiqlane").GetValue<KeyBind>().Active)
                                    q.Cast(qtarg.ServerPosition);
                            }

                            if (qtarg.IsValid<Obj_AI_Hero>() || qtarg.IsValid<Obj_AI_Turret>())
                            {
                                if (uo)
                                    q.Cast(qtarg.ServerPosition);
                            }
                        }
                    }
                }
            }        
        }

        #endregion

        #region Riven: Check R
        private static void checkr()
        {
            if (!r.IsReady() || uo || !menu.Item("user").GetValue<KeyBind>().Active)
                return;

            if (menulist("ultwhen") == 2 && cc <= menuslide("userq"))
                r.Cast();

            var targets = HeroManager.Enemies.Where(ene => ene.IsValidTarget(r.Range + 100));
            var heroes = targets as IList<Obj_AI_Hero> ?? targets.ToList();

            foreach (var target in heroes)
            {
                if (cc <= menuslide("userq") && (q.IsReady() || Utils.GameTimeTickCount - lastq < 1000))
                {
                    if (heroes.Count(ene => ene.Distance(player.ServerPosition) <= 750) > 1)
                        r.Cast();

                    if (heroes.Count() < 2 && target.Health/target.MaxHealth*100 <= menuslide("overk"))
                        return;

                    if (menulist("ultwhen") == 0)
                    {
                        if ((ComboDamage(target)/1.3) >= target.Health && target.Health >= (ComboDamage(target)/1.8))
                        {
                            r.Cast();
                        }
                    }

                    // hard kill ->
                    if (menulist("ultwhen") == 1)
                    {
                        if (ComboDamage(target) >= target.Health && target.Health >= ComboDamage(target)/1.8)
                        {
                            r.Cast();
                        }
                    }
                }
            }        
        }

        #endregion

        #region Riven: On Cast

        private static void OnCast()
        {
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
            {
                #region Riven Shield
                if (sender.IsEnemy && sender.Type == player.Type)
                {
                    var epos = player.ServerPosition +
                              (player.ServerPosition - sender.ServerPosition).Normalized()*300;

                    if (player.Distance(sender.ServerPosition) <= args.SData.CastRange)
                    {
                        switch (args.SData.TargettingType)
                        {
                            case SpellDataTargetType.Unit:

                                if (args.Target.NetworkId == player.NetworkId && menubool("ashield"))
                                {
                                    if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                                    {
                                        e.Cast(epos);
                                    }
                                }

                                break;
                            case SpellDataTargetType.SelfAoe:

                                if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit && menubool("bshield"))
                                {
                                    e.Cast(epos);
                                }

                                break;
                        }
                    }
                }

                #endregion

                if (!sender.IsMe)
                {
                    return;
                }


                if (args.SData.Name.ToLower().Contains("ward"))
                    lastwd = Utils.GameTimeTickCount;

                switch (args.SData.Name)
                {
                    case "RivenTriCleave":
                        cc += 1;
                        didq = true;
                        didaa = false;
                        canmv = false;
                        lastq = Utils.GameTimeTickCount;
                        canq = false;

                        if (cc >= 2)
                            Utility.DelayAction.Add(425 + (100 - Game.Ping / 2),
                                () => Orbwalking.LastAATick = 0);

                        if (!uo) ssfl = false;

                        // cancel q animation
                        Utility.DelayAction.Add(100 + (100 - Game.Ping / 2),
                            () => player.IssueOrder(GameObjectOrder.MoveTo, movepos));

                        break;
                    case "RivenMartyr":
                        didw = true;
                        lastw = Utils.GameTimeTickCount;
                        canw = false;

                        if (menulist("wmode") == 1)
                        {
                            if (!menu.Item("shycombo").GetValue<KeyBind>().Active && 
                                 menu.Item("combokey").GetValue<KeyBind>().Active && !canburst())
                            {
                                if (canhd) return;

                                if (riventarget() != null)
                                {
                                    Utility.DelayAction.Add(Game.Ping + 130, () => q.Cast(riventarget().ServerPosition));
                                    return;
                                }

                                if (orbwalker.GetTarget() != null)
                                {
                                    Utility.DelayAction.Add(Game.Ping + 130, () => q.Cast(orbwalker.GetTarget().Position));
                                }
                            }
                        }

                        break;
                    case "RivenFeint":
                        dide = true;
                        didaa = false;
                        laste = Utils.GameTimeTickCount;
                        cane = false;

                        if (menu.Item("fleekey").GetValue<KeyBind>().Active)
                        {
                            if (uo && r.IsReady() && cc == 2 && q.IsReady())
                            {
                                r.Cast(Game.CursorPos);
                            }
                        }

                        if (menu.Item("combokey").GetValue<KeyBind>().Active)
                        {
                            if (cc == 2 && !uo)
                            {
                                checkr();
                                Utility.DelayAction.Add(Game.Ping + 200, () => q.Cast(Game.CursorPos));
                            }

                            if (menulist("wsmode") == 1 && cc == 2 && uo)
                            {
                                if (riventarget().IsValidTarget(r.Range + 100) && IsLethal(riventarget()))
                                {
                                    Utility.DelayAction.Add(100 + Game.Ping,
                                    () => r.Cast(r.CastIfHitchanceEquals(riventarget(), HitChance.Medium)));
                                }
                            }
                        }

                        break;
                    case "RivenFengShuiEngine":
                        ssfl = true;

                        if (riventarget() != null && canburst(true))
                        {
                            if (!flash.IsReady() || menulist("multib") == 2)
                                return;

                            if (menu.Item("shycombo").GetValue<KeyBind>().Active)
                            {
                                if (riventarget().Distance(player.ServerPosition) > e.Range + 50 &&
                                    riventarget().Distance(player.ServerPosition) <= e.Range + wrange + 300)
                                {
                                    var second =
                                        HeroManager.Enemies.Where(
                                            x => x.NetworkId != riventarget().NetworkId &&
                                                 x.Distance(riventarget().ServerPosition) <= r.Range)
                                            .OrderByDescending(xe => xe.Distance(riventarget().ServerPosition))
                                            .FirstOrDefault();

                                    if (second != null)
                                    {
                                        var pos = riventarget().ServerPosition +
                                                  (riventarget().ServerPosition - second.ServerPosition).Normalized() * 75;

                                        player.Spellbook.CastSpell(flash, pos);
                                    }

                                    else
                                    {
                                        player.Spellbook.CastSpell(flash,
                                            riventarget().ServerPosition.Extend(player.ServerPosition, 115));
                                    }
                                }
                            }
                        }

                        break;
                    case "rivenizunablade":
                        ssfl = false;
                        didws = true;
                        canws = false;

                        if (w.IsReady() && riventarget().IsValidTarget(wrange))
                            w.Cast();

                        if (q.IsReady() && riventarget().IsValidTarget())
                            q.Cast(riventarget().ServerPosition);

                        break;
                    case "ItemTiamatCleave":
                    case "ItemTitanicHydraCleave":
                        lasthd = Utils.GameTimeTickCount;
                        didhd = true;
                        canws = true;
                        canhd = false;

                        if (menulist("wsmode") == 1 && uo && canws)
                        {
                            if (menu.Item("combokey").GetValue<KeyBind>().Active)
                            {
                                if (canburst())
                                {
                                    if (riventarget().IsValidTarget() && !riventarget().IsZombie)
                                    {
                                        Utility.DelayAction.Add(125 + Game.Ping,
                                            () => r.Cast(r.CastIfHitchanceEquals(riventarget(), HitChance.Medium)));
                                    }
                                }
                            }

                            if (menu.Item("shycombo").GetValue<KeyBind>().Active)
                            {
                                if (canburst(true))
                                {
                                    if (riventarget().IsValidTarget() && !riventarget().IsZombie)
                                    {
                                        Utility.DelayAction.Add(125 + Game.Ping,
                                            () => r.Cast(r.CastIfHitchanceEquals(riventarget(), HitChance.Medium)));
                                    }
                                }
                            }
                        }

                        if (menulist("emode") == 1)
                        {
                            if (menu.Item("combokey").GetValue<KeyBind>().Active)
                            {
                                checkr();
                                Utility.DelayAction.Add(Game.Ping + 175, () => q.Cast(Game.CursorPos));
                            }
                        }

                        break;
                    default:
                        if (args.SData.Name.Contains("Attack"))
                        {
                            if (menu.Item("combokey").GetValue<KeyBind>().Active || menu.Item("shycombo").GetValue<KeyBind>().Active)
                            {
                                if (canburst() || menulist("emode") == 0 && !canburst() ||
                                    menu.Item("shycombo").GetValue<KeyBind>().Active)
                                {
                                    // delay till after aa
                                    Utility.DelayAction.Add(
                                        50 + (int) (player.AttackDelay * 100) + Game.Ping / 2 + menuslide("autoaq"), delegate
                                        {
                                            if (Items.CanUseItem(3077))
                                                Items.UseItem(3077);
                                            if (Items.CanUseItem(3074))
                                                Items.UseItem(3074);
                                            if (Items.CanUseItem(3748))
                                                Items.UseItem(3748);
                                        });
                                }

                                else if (!menubool("usecombow") || !menubool("usecomboe"))
                                {
                                    // delay till after aa
                                    Utility.DelayAction.Add(
                                        50 + (int) (player.AttackDelay * 100) + Game.Ping / 2 + menuslide("autoaq"), delegate
                                        {
                                            if (Items.CanUseItem(3077))
                                                Items.UseItem(3077);
                                            if (Items.CanUseItem(3074))
                                                Items.UseItem(3074);
                                            if (Items.CanUseItem(3748))
                                                Items.UseItem(3748);
                                        });
                                }
                            }

                            else if (menu.Item("clearkey").GetValue<KeyBind>().Active)
                            {
                                if (qtarg.IsValid<Obj_AI_Base>() && !qtarg.Name.StartsWith("Minion"))
                                {
                                    Utility.DelayAction.Add(
                                        50 + (int) (player.AttackDelay*100) + Game.Ping/2 + menuslide("autoaq"), delegate
                                        {
                                            if (Items.CanUseItem(3077))
                                                Items.UseItem(3077);
                                            if (Items.CanUseItem(3074))
                                                Items.UseItem(3074);
                                            if (Items.CanUseItem(3748))
                                                Items.UseItem(3748);
                                        });
                                }
                            }
                        }
                        break;
                }

                if (!didq && args.SData.Name.Contains("Attack"))
                {
                    didaa = true;
                    canaa = false;
                    canq = false;
                    canw = false;
                    cane = false;
                    canws = false;
                    lastaa = Utils.GameTimeTickCount;
                    qtarg = (Obj_AI_Base) args.Target;
                }
            };
        }

        #endregion

        #region Riven: Misc Events
        private static void Interrupter()
        {
            Interrupter2.OnInterruptableTarget += (sender, args) =>
            {
                if (menubool("wint") && w.IsReady())
                {
                    if (!sender.Position.UnderTurret(true))
                    {
                        if (sender.IsValidTarget(w.Range))
                            w.Cast();

                        if (sender.IsValidTarget(w.Range + e.Range) && e.IsReady())
                        {
                            e.Cast(sender.ServerPosition);
                        }
                    }
                }

                if (menubool("qint") && q.IsReady() && cc >= 2)
                {
                    if (!sender.Position.UnderTurret(true))
                    {
                        if (sender.IsValidTarget(q.Range))
                            q.Cast(sender.ServerPosition);

                        if (sender.IsValidTarget(q.Range + e.Range) && e.IsReady())
                        {
                            e.Cast(sender.ServerPosition);
                        }
                    }
                }
            };
        }

        private static void OnGapcloser()
        {
            AntiGapcloser.OnEnemyGapcloser += gapcloser =>
            {
                if (menubool("wgap") && w.IsReady())
                {
                    if (gapcloser.Sender.IsValidTarget(w.Range))
                    {
                        if (!gapcloser.Sender.ServerPosition.UnderTurret(true))
                        {
                            w.Cast();
                        }
                    }               
                }
            };
        }

        private void OnPlayAnimation()
        {
            Obj_AI_Base.OnPlayAnimation += (sender, args) =>
            {
                if (!(didq || didw || didws || dide) && !player.IsDead)
                {
                    if (sender.IsMe)
                    {
                        if (args.Animation.Contains("Idle"))
                        {
                            canq = false;
                            canaa = true;
                        }

                        if (args.Animation.Contains("Run"))
                        {
                            canq = false;
                            canaa = true;
                        }
                    }
                }

            };
        }

        #endregion

        #region Riven: Aura

        private static void AuraUpdate()
        {
            if (!player.IsDead)
            {
                foreach (var buff in player.Buffs)
                {
                    //if (buff.Name == "RivenTriCleave")
                    //    cc = buff.Count;

                    if (buff.Name == "rivenpassiveaaboost")
                        pc = buff.Count;
                }

                if (player.HasBuff("RivenTriCleave", true))
                {
                    if (Utils.GameTimeTickCount - lastq >= 3650)
                    {
                        if (!player.IsRecalling() && !player.Spellbook.IsChanneling)
                        {
                            var qext = player.ServerPosition.To2D() + 
                                       player.Direction.To2D().Perpendicular() * q.Range + 100;

                            if (menubool("keepq") && !qext.To3D().UnderTurret(true))
                                q.Cast(Game.CursorPos);
                        }
                    }
                }

                if (!player.HasBuff("rivenpassiveaaboost", true))
                    Utility.DelayAction.Add(1000, () => pc = 1);

                if (cc > 2)
                    Utility.DelayAction.Add(1000, () => cc = 0);
            }
        }

        #endregion

        #region Riven : Combat/Orbwalk

        private static void OrbTo(Obj_AI_Base target, float rangeoverride = 0f)
        {
            if (canmv)
            {
                if (menu.Item("shycombo").GetValue<KeyBind>().Active)
                {
                    if (target.IsValidTarget(600))
                        Orbwalking.Orbwalk(target, Game.CursorPos, 80f, 0f, false, false);

                    else
                        player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                }
            }

            if (canmv)
            {
                if (q.IsReady())
                {
                    if (target.IsValidTarget(truerange + rangeoverride))
                    {
                        Orbwalking.LastAATick = 0;
                    }
                }
            }
        }

        private static void CombatCore()
        {
            if (didhd && canhd && Utils.GameTimeTickCount - lasthd >= 250)
                didhd = false;

            if (didq)
            {
                if (Utils.GameTimeTickCount - lastq >= 500 + Game.Ping/2)
                {
                    didq = false;
                    canmv = true;
                    canaa = true;
                }
            }

            if (didw)
            {
                if (Utils.GameTimeTickCount - lastw >= 266)
                {
                    didw = false;
                    canmv = true;
                    canaa = true;
                }
            }

            if (dide)
            {
                if (Utils.GameTimeTickCount - laste >= 300)
                {
                    dide = false;
                    canmv = true;
                    canaa = true;
                }             
            }

            if (didaa)
            {

                if (Utils.GameTimeTickCount - lastaa >= 25 + (player.AttackDelay *100) + Game.Ping/2 + menuslide("autoaq"))
                {
                    didaa = false;
                    canmv = true;
                    canq = true;
                    cane = true;
                    canw = true;
                    canws = true;
                }
            }

            if (!canw && w.IsReady())
            {
                if (!(didaa || didq || dide))
                {
                    canw = true;
                }
            }

            if (!cane && e.IsReady())
            {
                if (!(didaa || didq || didw))
                {
                    cane = true;
                }
            }

            if (!canws && r.IsReady())
            {
                if (!(didaa || didw) && uo)
                {
                    canws = true;
                }
            }

            if (!canaa)
            {
                if (!(didq || didw|| dide || didws || didhd || didhs))
                {
                    if (Utils.GameTimeTickCount - lastaa >= 1000)
                    {
                        canaa = true;
                    }
                }
            }

            if (!canmv)
            {
                if (!(didq || didw || dide || didws || didhd || didhs))
                {
                    if (Utils.GameTimeTickCount - lastaa >= 1100)
                    {
                        canmv = true;
                    }
                }
            }
        }

        #endregion

        #region Riven: Math/Damage

        private static float ComboDamage(Obj_AI_Base target)
        {
            if (target == null)
                return 0f;

            var ignote = player.GetSpellSlot("summonerdot");
            var ad = (float)player.GetAutoAttackDamage(target);
            var runicpassive = new[] { 0.2, 0.25, 0.3, 0.35, 0.4, 0.45, 0.5 };

            var ra = ad +
                        (float)
                            ((+player.FlatPhysicalDamageMod + player.BaseAttackDamage) *
                            runicpassive[player.Level / 3]);

            var rw = Wdmg(target);
            var rq = Qdmg(target);
            var rr = r.IsReady() ? r.GetDamage(target) : 0;

            var ii = (ignote != SpellSlot.Unknown && player.GetSpell(ignote).State == SpellState.Ready && r.IsReady()
                ? player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite)
                : 0);

            var tmt = Items.HasItem(3077) && Items.CanUseItem(3077)
                ? player.GetItemDamage(target, Damage.DamageItems.Tiamat)
                : 0;

            var hyd = Items.HasItem(3074) && Items.CanUseItem(3074)
                ? player.GetItemDamage(target, Damage.DamageItems.Hydra)
                : 0;

            var tdh = Items.HasItem(3748) && Items.CanUseItem(3748)
                ? player.GetItemDamage(target, Damage.DamageItems.Hydra)
                : 0;

            var bwc = Items.HasItem(3144) && Items.CanUseItem(3144)
                ? player.GetItemDamage(target, Damage.DamageItems.Bilgewater)
                : 0;

            var brk = Items.HasItem(3153) && Items.CanUseItem(3153)
                ? player.GetItemDamage(target, Damage.DamageItems.Botrk)
                : 0;

            var items = tmt + hyd + tdh + bwc + brk;

            var damage = (rq * 3 + ra * 3 + rw + rr + ii + items);

            return xtra((float) damage);
        }


        private static double Wdmg(Obj_AI_Base target)
        {
            double dmg = 0;
            if (w.IsReady() && target != null)
            {
                dmg += player.CalcDamage(target, Damage.DamageType.Physical,
                    new[] {50, 80, 110, 150, 170}[w.Level - 1] + 1*player.FlatPhysicalDamageMod + player.BaseAttackDamage);
            }

            return dmg;
        }

        private static double Qdmg(Obj_AI_Base target)
        {
            double dmg = 0;
            if (q.IsReady() && target != null)
            {
                dmg += player.CalcDamage(target, Damage.DamageType.Physical,
                    -10 + (q.Level * 20) + (0.35 + (q.Level * 0.05)) * (player.FlatPhysicalDamageMod + player.BaseAttackDamage));
            }

            return dmg;
        }

        #endregion

        #region Riven: Drawings

        private static void Drawings()
        {
            Drawing.OnDraw += args =>
            {
                if (!player.IsDead) 
                {
                    if (_sh.IsValidTarget())
                    {
                        if (menu.Item("drawf").GetValue<Circle>().Active)
                            Render.Circle.DrawCircle(_sh.Position, 200, menu.Item("drawf").GetValue<Circle>().Color, 6);
                    }

                    if (menu.Item("drawengage").GetValue<Circle>().Active)
                    {
                        Render.Circle.DrawCircle(player.Position,
                            player.AttackRange + e.Range + 35, menu.Item("drawengage").GetValue<Circle>().Color,
                            menu.Item("linewidth").GetValue<Slider>().Value);
                    }

                    if (menu.Item("drawr2").GetValue<Circle>().Active)
                    {
                        Render.Circle.DrawCircle(player.Position, r.Range, menu.Item("drawr2").GetValue<Circle>().Color,
                            menu.Item("linewidth").GetValue<Slider>().Value);
                    }

                    if (menu.Item("drawburst").GetValue<Circle>().Active && canburst(true) && riventarget().IsValidTarget())
                    {
                        var xrange = menulist("multib") != 2 && flash.IsReady() ? 300 : 0;
                        Render.Circle.DrawCircle(riventarget().Position, e.Range + 75 + xrange,
                            menu.Item("drawengage").GetValue<Circle>().Color, menu.Item("linewidth").GetValue<Slider>().Value);
                    }
                }
            };

            Drawing.OnEndScene += args =>
            {
                if (!menubool("drawdmg"))
                    return;

                foreach (
                    var enemy in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(ene => ene.IsValidTarget() && !ene.IsZombie))
                {
                    var color = r.IsReady() && IsLethal(enemy)
                        ? new ColorBGRA(0, 255, 0, 90)
                        : new ColorBGRA(255, 255, 0, 90);

                    hpi.unit = enemy;
                    hpi.drawDmg(ComboDamage(enemy), color);
                }

            };
        }

        #endregion

    }
}
