using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SharpDX;
using Color = System.Drawing.Color;
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
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (
                var enemyVisible in ObjectManager.Get<Obj_AI_Hero>().Where(enemyVisible => enemyVisible.IsValidTarget())
                )
            {
                if (Player.HasBuff("rengarr") && PrioDamage(enemyVisible) > enemyVisible.Health)
                {
                    Drawing.DrawText(
                        Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                        Drawing.WorldToScreen(enemyVisible.Position)[1] - 40,
                        Color.Red,
                        "Ulti Combo TEQ");
                }
                else if (Player.HasBuff("rengarr") && PrioDamage(enemyVisible) + Player.GetAutoAttackDamage(enemyVisible, true) * 2 > enemyVisible.Health)
                {
                    Drawing.DrawText(
                        Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                        Drawing.WorldToScreen(enemyVisible.Position)[1] - 40,
                        Color.DarkOrange,
                        "Ulti Combo KILL");
                }

                else if (Player.HasBuff("rengarr") && PrioDamage(enemyVisible) + Player.GetAutoAttackDamage(enemyVisible, true) * 3.65 > enemyVisible.Health)
                {
                    Drawing.DrawText(
                        Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                        Drawing.WorldToScreen(enemyVisible.Position)[1] - 40,
                        Color.DarkCyan,
                        "Ulti Combo Orta KILL");
                }
                if (ComboDamage(enemyVisible) > enemyVisible.Health && !Player.HasBuff("rengarr"))
                {
                    Drawing.DrawText(
                        Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                        Drawing.WorldToScreen(enemyVisible.Position)[1] - 40,
                        Color.Red,
                        "Direk Combo");
                }
                else if (ComboDamage(enemyVisible) + Player.GetAutoAttackDamage(enemyVisible, true) * 2.6 > enemyVisible.Health && !Player.HasBuff("rengarr"))
                {
                    Drawing.DrawText(
                        Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                        Drawing.WorldToScreen(enemyVisible.Position)[1] - 40,
                        Color.DarkOrange,
                        "Combo + Tiamat = Teq");
                }
                else
                    Drawing.DrawText(
                        Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                        Drawing.WorldToScreen(enemyVisible.Position)[1] - 40,
                        Color.Green,
                        "Alamazsin");
            }
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
            Notifications.AddNotification(string.Format("Coded by Rexy-"), 10500);
            Menu = new Menu("Tek Atan Rengar", "Tek Atan Rengar", true);
            var orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            Menu.AddSubMenu(orbwalkerMenu);
            Menu.SubMenu("Combo Modu")
                .AddItem(
                    new MenuItem("ComboMode", "Combo Modu").SetValue(
                        new StringList(new[] { "TEQ", "LANE" })));
            Menu.SubMenu("Otomatik Can")
                .AddItem(new MenuItem("autoheal", "Otomatik Can Icin Yuzde").SetValue(new Slider(30, 100, 22)));
            var kıredit = new Menu("Coded by Rexy", "Coded by Rexy");
            Menu.AddSubMenu(kıredit);
            Menu.AddToMainMenu();
            
            Game.OnUpdate += Game_OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += oncast;
        }

        #region ComboDamage

        private static float ComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;
            var _igniteSlot = Player.GetSpellSlot("SummonerDot");
            if (_igniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready) damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);
            if (Items.HasItem(3077) && Items.CanUseItem(3077)) damage += Player.GetItemDamage(enemy, Damage.DamageItems.Tiamat);
            if (Items.HasItem(3074) && Items.CanUseItem(3074)) damage += Player.GetItemDamage(enemy, Damage.DamageItems.Hydra);
            if (Q.IsReady()) damage += Player.GetSpellDamage(enemy, SpellSlot.Q);
            if (W.IsReady()) damage += Player.GetSpellDamage(enemy, SpellSlot.W);
            if (E.IsReady()) damage += Player.GetSpellDamage(enemy, SpellSlot.E);
            damage += (damage - ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite));
            return (float)damage;
        }
        #endregion
        private static float PrioDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;
            var _igniteSlot = Player.GetSpellSlot("SummonerDot");
            if (_igniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready) damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);
            if (Items.HasItem(3077) && Items.CanUseItem(3077)) damage += Player.GetItemDamage(enemy, Damage.DamageItems.Tiamat);
            if (Items.HasItem(3074) && Items.CanUseItem(3074)) damage += Player.GetItemDamage(enemy, Damage.DamageItems.Hydra);
            if (Q.IsReady()) damage += Player.GetSpellDamage(enemy, SpellSlot.Q);
            if (Q.IsReady()) damage += (Player.GetSpellDamage(enemy, SpellSlot.Q) / 1.5 );
            if (W.IsReady()) damage += Player.GetSpellDamage(enemy, SpellSlot.W);
            if (E.IsReady()) damage += Player.GetSpellDamage(enemy, SpellSlot.E);
            damage += (damage - ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite));
            return (float)damage;
        }
        public static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            var hp = Menu.Item("autoheal").GetValue<Slider>().Value;

            DrawSelectedTarget();

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
                            if (searchtarget.Distance(ObjectManager.Player.Position) > 250 && searchtarget.Distance(ObjectManager.Player.Position) < 1000)
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
        private static void DrawSelectedTarget()
        {
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
    }
}
