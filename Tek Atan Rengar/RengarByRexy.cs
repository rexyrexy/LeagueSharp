using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using ItemData = LeagueSharp.Common.Data.ItemData;

namespace Tek_Atan_Rengar
{
    internal class RengarByRexy
    {
        private static String championName = "Rengar";

        public static Obj_AI_Hero Player;

        private static Menu Menu;

        private static Orbwalking.Orbwalker orbwalker;

        private static Spell Q, W, E, R;
        private static string mode
        {
            get
            {
                return Menu.Item("ComboMode").GetValue<StringList>().SelectedValue;
            }
        }

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            Game.OnUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += oncast;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {

            Player = ObjectManager.Player;
            if (Player.ChampionName != championName)
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 250);
            W = new Spell(SpellSlot.W, 350);
            E = new Spell(SpellSlot.E, 1000);
            R = new Spell(SpellSlot.R, 1500);

            E.SetSkillshot(0.25f, 70, 1500, true, SkillshotType.SkillshotLine);
            E.MinHitChance = HitChance.Medium;

            Notifications.AddNotification(string.Format("Tek Atan Rengar Yuklendi !"), 10500);
            Notifications.AddNotification(string.Format("Coded by Rexy"), 10500);
            Menu = new Menu("Tek Atan Rengar", "Tek Atan Rengar", true);
            var orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            Menu.AddSubMenu(orbwalkerMenu);
            Menu.SubMenu("Combo Modu").AddItem(new MenuItem("ComboMode", "Combo Modu").SetValue(new StringList(new[] { "TEQ", "LANE" })));
            Menu.SubMenu("Combo Modu").AddItem(new MenuItem("eqr", "Menzil Disinda E Kullanma").SetValue(true));
            Menu.SubMenu("Otomatik Can").AddItem(new MenuItem("autoheal", "Otomatik Can Icin Yuzde").SetValue(new Slider(30, 100, 22)));
            Menu.AddToMainMenu();
            Game.OnUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += oncast;
        }

        public static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            var hp = Menu.Item("autoheal").GetValue<Slider>().Value;

            if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }

            if (Player.Mana == 5 && W.IsReady())
            {
                if ((Player.Health / Player.MaxHealth * 100) < hp)
                {
                    W.Cast();
                }
            }
        }
        public static void oncast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var spell = args.SData;
            if (!sender.IsMe)
                return;
            if (spell.Name.ToLower().Contains("rengarq"))
            {
                Orbwalking.ResetAutoAttackTimer();
            }
            if (ObjectManager.Player.HasBuff("rengarqbase") || ObjectManager.Player.HasBuff("rengarqemp"))
            {
                Orbwalking.ResetAutoAttackTimer();
            }
        }
        private static void Combo()
        {
            var searchtarget = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);
            var closetarget = TargetSelector.GetTarget(350, TargetSelector.DamageType.Physical);

            if (searchtarget.IsValidTarget(500))
            {
                Items.UseItem(3144, searchtarget);
                Items.UseItem(3146, searchtarget);
                Items.UseItem(3153, searchtarget);
            }
            if (closetarget.IsValidTarget(350))
            {
                Items.UseItem(3074);
                Items.UseItem(3077);
                Items.UseItem(3143);
            }

			if (Player.HasBuff("rengarr"))
			{
				return;
			}
			
            if (ObjectManager.Player.Mana <= 4)
            {
                if (searchtarget.IsValidTarget(1000) && (ObjectManager.Player.HasBuff("rengarpassivebuff") || ObjectManager.Player.HasBuff("rengarbushspeedbuff") || ObjectManager.Player.HasBuff("rengarr")))
                {
                    Q.Cast();
                }
                if (searchtarget.IsValidTarget(800) && (ObjectManager.Player.HasBuff("rengarpassivebuff") || ObjectManager.Player.HasBuff("rengarbushspeedbuff") || ObjectManager.Player.HasBuff("rengarr")))
                {
                    Items.UseItem(3142);
                }
                if (searchtarget.IsValidTarget(1000) && !ObjectManager.Player.HasBuff("rengarpassivebuff") && !ObjectManager.Player.HasBuff("rengarbushspeedbuff") && !ObjectManager.Player.HasBuff("rengarr"))
                {
                    E.Cast(searchtarget);
                    if (mode == "LANE")
                    {
                        if ((Q.IsReady() || W.IsReady() || E.IsReady()) && closetarget.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 100)
                        {
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, closetarget);
                        }
                    }
                    if (mode == "TEQ")
                    {
                        if (closetarget.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 100)
                        {
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, closetarget);
                        }
                    }
                }
                if (closetarget.IsValidTarget(Q.Range))
                {
                    Q.Cast(closetarget);
                }
                if (closetarget.IsValidTarget(350))
                {
                    E.Cast(closetarget);
                    W.Cast(closetarget);
                    if (mode == "LANE")
                    {
                        if ((Q.IsReady() || W.IsReady() || E.IsReady()) && closetarget.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 100)
                        {
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, closetarget);
                        }
                    }
                    if (mode == "TEQ")
                    {
                        if (closetarget.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 100)
                        {
                            ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, closetarget);
                        }
                    }
                }
            }
            if (ObjectManager.Player.Mana == 5)
            {
                var eqq = Menu.Item("eqr").GetValue<bool>();
                if (mode == "LANE")
                        {
                           
                            if (searchtarget.IsValidTarget(1000) && (ObjectManager.Player.HasBuff("rengarpassivebuff") || ObjectManager.Player.HasBuff("rengarbushspeedbuff") || ObjectManager.Player.HasBuff("rengarr")))
                            {
                                Q.Cast();
                            }
                            if (searchtarget.IsValidTarget(800) && (ObjectManager.Player.HasBuff("rengarpassivebuff") || ObjectManager.Player.HasBuff("rengarbushspeedbuff") || ObjectManager.Player.HasBuff("rengarr")))
                            {
                                Items.UseItem(3142);
                            }
                            if (closetarget.IsValidTarget(Q.Range))
                            {
                                Q.Cast(closetarget);
                            }
                            if (eqq && searchtarget.Distance(ObjectManager.Player.Position) > 250 && searchtarget.Distance(ObjectManager.Player.Position) < 1000)
                            {
                                E.Cast(searchtarget);
                            }
                            if (mode == "LANE")
                            {
                                if ((Q.IsReady() || W.IsReady() || E.IsReady()) && closetarget.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 100)
                                {
                                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, closetarget);
                                }
                            }
                            if (mode == "TEQ")
                            {
                                if (closetarget.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 100)
                                {
                                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, closetarget);
                                }
                            }
                        }
                    else if (mode == "TEQ")
                        {
                            if (searchtarget.IsValidTarget(800) && (ObjectManager.Player.HasBuff("rengarpassivebuff") || ObjectManager.Player.HasBuff("rengarbushspeedbuff") || ObjectManager.Player.HasBuff("rengarr")))
                            {
                                Items.UseItem(3142);
                            }
                            if (searchtarget.IsValidTarget(1000) && !ObjectManager.Player.HasBuff("rengarpassivebuff") && !ObjectManager.Player.HasBuff("rengarbushspeedbuff") && !ObjectManager.Player.HasBuff("rengarr"))
                            {
                                E.Cast(searchtarget);
                            }
                            if (closetarget.IsValidTarget(350))
                            {
                                E.Cast(closetarget);
                            }
                            if (mode == "LANE")
                            {
                                if ((Q.IsReady() || W.IsReady() || E.IsReady()) && closetarget.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 100)
                                {
                                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, closetarget);
                                }
                            }
                            if (mode == "TEQ")
                            {
                                if (closetarget.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 100)
                                {
                                    ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, closetarget);
                                }
                            }
                        }
                }
            }
            /*
            var target = TargetSelector.GetSelectedTarget();
            if (target != null)
            {
                if (notifyselected.Text == target.ChampionName)
                {
                    return;
                }
                else
                {
                    Notifications.RemoveNotification(notifyselected);
                    notifyselected = new Notification(target.ChampionName);
                    Notifications.AddNotification(notifyselected);
                }
            }
            else
            {
                if (notifyselected.Text == "Yok")
                {
                    return;
                }
                else
                {
                    Notifications.RemoveNotification(notifyselected);
                    notifyselected = new Notification("Yok");
                    Notifications.AddNotification(notifyselected);
                }
            }
        }
        private static Notification notifyselected = new Notification("Yok");
    */
    }
}
