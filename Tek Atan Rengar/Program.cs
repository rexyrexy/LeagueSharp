using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.Threading.Tasks;
using System.Text;
using SharpDX;
using Color = System.Drawing.Color;
using System.Drawing.Text;
using System.Xml.Xsl;

namespace Tek_Atan_Rengar
{
    internal class Program
    {
        private static String championName = "Rengar";

        public static Obj_AI_Hero Player;

        private static Menu menu;

        private static Orbwalking.Orbwalker orbwalker;

        private static Spell Q, W, E, R;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            Drawing.OnDraw += Drawing_OnDraw;

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (
                var enemyVisible in ObjectManager.Get<Obj_AI_Hero>().Where(enemyVisible => enemyVisible.IsValidTarget()))
            {
                if (ComboDamage(enemyVisible) > enemyVisible.Health)
                {
                    Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50, Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Red, "Teq");
                }
                else if (ComboDamage(enemyVisible) + Player.GetAutoAttackDamage(enemyVisible, true) * 2 > enemyVisible.Health)
                {
                    Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50, Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Orange, "Combo + 2 AA");
                }
                else
                    Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50, Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Green, "Ölemez");
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
            W = new Spell(SpellSlot.W, 500);
            E = new Spell(SpellSlot.E, 1000);

            E.SetSkillshot(0.25f, 70, 1500, true, SkillshotType.SkillshotLine);
            E.MinHitChance = HitChance.High;

            Notifications.AddNotification(string.Format("Tek Atan Rengar Yuklendi !"), 5500);
            Notifications.AddNotification(string.Format("Berk Gunay"), 5500);
            menu = new Menu("Tek Atan Rengar", "Tek Atan Rengar", true);
            var orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            menu.AddSubMenu(orbwalkerMenu);
            TargetSelector.AddToMenu(menu);
            menu.SubMenu("Combo Modu").AddItem(new MenuItem("ComboMode", "Combo Modu").SetValue(new StringList(new[] { "Sadece Q", "Menzil disinda E"})));
            menu.SubMenu("Otomatik Can").AddItem(new MenuItem("autoheal", "Otomatik Can Icin Yuzde").SetValue(new Slider(22, 100, 0)));
            menu.AddToMainMenu();
        
            if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }
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

        #region Combo 


        private static void Combo()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(2300, TargetSelector.DamageType.Physical);
            var hp = menu.Item("autoheal").GetValue<Slider>().Value;
            var skill = menu.Item("ComboMode").GetValue<StringList>().SelectedIndex;
            switch (skill)
            {
                case 0:
                    {
                        if (Player.Mana == 5)
                        {
                            if ((ObjectManager.Player.Health / ObjectManager.Player.MaxHealth) * 100 < hp && W.IsReady())
                            {
                                W.Cast();
                            }

                            if (target.IsValidTarget(Q.Range) && target.IsEnemy && !target.IsDead && Q.IsReady())
                            {
                                Q.Cast(target);
                            }

                        }

                        else if (Player.Mana <= 4)
                        {
                            if (target.IsValidTarget(E.Range) && target.IsEnemy && !target.IsDead)
                            {
                                Q.Cast(target);
                                E.Cast(target);
                                W.Cast(target);
                            }
                        }
                    }
                    break;
                case 1:
                    {
                        if (Player.Mana == 5)
                        {
                            if ((ObjectManager.Player.Health / ObjectManager.Player.MaxHealth) * 100 < hp && W.IsReady())
                            {
                                W.Cast();
                            }

                            if (target.IsValidTarget(Q.Range) && target.IsEnemy && !target.IsDead && Q.IsReady())
                            {
                                Q.Cast(target);
                            }

                            else if (!target.IsValidTarget(Q.Range) && target.IsValidTarget(E.Range) && target.IsEnemy
                                     && !target.IsDead && E.IsReady())
                            {
                                E.Cast(target);
                            }
                             
                        }

                        else if (Player.Mana <= 4)
                        {
                            if (target.IsValidTarget(E.Range) && target.IsEnemy && !target.IsDead)
                            {
                                Q.Cast(target);
                                E.Cast(target);
                                W.Cast(target);
                            }
                        }
                    }
                    break;
            }
        }
        #endregion
}
}