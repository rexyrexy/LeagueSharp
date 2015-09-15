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

        private static Spell Q, W, E;

        private static string mode
        {
            get
            {
                return Menu.Item("ComboMode").GetValue<StringList>().SelectedValue;
            }
        }

        private static int extrawindup = 50;

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
                if (ComboDamage(enemyVisible) > enemyVisible.Health)
                {
                    Drawing.DrawText(
                        Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                        Drawing.WorldToScreen(enemyVisible.Position)[1] - 40,
                        Color.Red,
                        "Teq");
                }
                else if (ComboDamage(enemyVisible) + Player.GetAutoAttackDamage(enemyVisible, true) * 2
                         > enemyVisible.Health)
                {
                    Drawing.DrawText(
                        Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                        Drawing.WorldToScreen(enemyVisible.Position)[1] - 40,
                        Color.Orange,
                        "Combo + 2 AA");
                }
                else
                    Drawing.DrawText(
                        Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                        Drawing.WorldToScreen(enemyVisible.Position)[1] - 40,
                        Color.Green,
                        "No Kill");
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

            E.SetSkillshot(0.25f, 70, 1500, true, SkillshotType.SkillshotLine);
            E.MinHitChance = HitChance.Medium;

            Notifications.AddNotification(string.Format("Tek Atan Rengar Yuklendi !"), 10500);
            Notifications.AddNotification(string.Format("Coded-by-Rexy-"), 10500);
            Menu = new Menu("Tek Atan Rengar", "Tek Atan Rengar", true);
            var orbwalkerMenu = new Menu("Orbwalker", "Orbwalker");
            orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            Menu.AddSubMenu(orbwalkerMenu);
            TargetSelector.AddToMenu(Menu);
            Menu.SubMenu("Combo Modu")
                .AddItem(
                    new MenuItem("ComboMode", "Combo Modu").SetValue(
                        new StringList(new[] { "Sadece Q", "Menzil disinda E" })));
            Menu.SubMenu("Otomatik Can")
                .AddItem(new MenuItem("autoheal", "Otomatik Can Icin Yuzde").SetValue(new Slider(22, 100, 22)));
            Menu.AddToMainMenu();

            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
            Obj_AI_Base.OnBuffRemove += Obj_AI_Base_OnBuffRemove;
            Game.OnUpdate += Game_OnGameUpdate;
            Orbwalking.AfterAttack += AfterAttack;
            Obj_AI_Base.OnProcessSpellCast += oncast;

            Console.WriteLine("|Tek Atan Rengar| Script Yuklendi !");
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

        private static void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && !Player.HasBuff("rengarpassivebuff")
                && Q.IsReady() && !(Player.Mana == 5))
            {
                var x = Prediction.GetPrediction(args.Target as Obj_AI_Base, Player.AttackCastDelay + 0.04f);
                if (Player.Distance(x.UnitPosition)
                    <= Player.BoundingRadius + Player.AttackRange + args.Target.BoundingRadius)
                {
                    args.Process = false;
                    Q.Cast();
                }
            }
        }

        private static void Obj_AI_Base_OnBuffRemove(Obj_AI_Base sender, Obj_AI_BaseBuffRemoveEventArgs args)
        {
            if (!sender.IsMe) return;
            if (args.Buff.Name == "rengarqbase" || args.Buff.Name == "rengarqemp")
            {

            }
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
            Console.WriteLine("|Tek Atan Rengar| Oyun Basladi !");
        }

        public static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe) return;
            if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (mode == "Menzil disinda E" && Player.Mana == 5)
                {
                    if (HasItem())
                        CastItem();
                }
                else if (Q.IsReady())
                {
                    Q.Cast();
                }
                else if (HasItem())
                {
                    CastItem();
                }
                else if (E.IsReady())
                {
                    var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                    if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                    {
                        E.Cast(targetE);
                    }
                    foreach (var tar in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                    {
                        if (E.IsReady())
                            E.Cast(tar);
                    }
                }
            }
        }

        public static void oncast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var spell = args.SData;
            if (!sender.IsMe)
                return;
            //Game.Say(spell.Name);
            if (spell.Name.ToLower().Contains("rengarq"))
            {
                //Game.PrintChat("reset");
                Orbwalking.ResetAutoAttackTimer();
            }
            //if (spell.Name.ToLower().Contains("rengarw")) ;
            if (spell.Name.ToLower().Contains("rengare"))
                if (Orbwalking.LastAATick < Utils.GameTimeTickCount - Game.Ping / 2 && Utils.GameTimeTickCount - Game.Ping / 2 < Orbwalking.LastAATick + Player.AttackCastDelay * 1000 + 40)
                {
                    Orbwalking.ResetAutoAttackTimer();
                }
        }
        public static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (!sender.IsMe)
                return;
            if (orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && HasItem())
            {
                if (args.Duration - 100 - Game.Ping / 2 > 0)
                {
                    Utility.DelayAction.Add((int)(/*Player.AttackCastDelay * 1000 + */args.Duration - 100 - Game.Ping / 2), CastItem);
                }
                else
                {
                    CastItem();
                }
            }
            //Game.Say("dash");
        }
        private static void Combo()
        {
            Obj_AI_Hero eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            Obj_AI_Base qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            Obj_AI_Base wTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);

            if (mode == "Menzil disinda E")
            {
                if (Player.Mana < 5)
                {
                    if (Q.IsReady() && Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) != 0)
                    {
                        if (Orbwalking.CanMove(extrawindup) && !Orbwalking.CanAttack() /*&& dontwaitQ*/)
                        {
                            Q.Cast();
                        }
                    }
                    if (Orbwalking.CanMove(extrawindup))
                    {
                        var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                        if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                        {
                            E.Cast(targetE);
                        }
                        foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                        {
                            if (E.IsReady())
                                E.Cast(target);
                        }
                    }
                    var targetW = TargetSelector.GetTarget(500, TargetSelector.DamageType.Physical);
                    if (W.IsReady() && targetW.IsValidTarget() && !targetW.IsZombie)
                    {
                        W.Cast(targetW);
                    } 
                }
                else
                {
                    if (Q.IsReady() && Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) != 0)
                    {
                        if (Orbwalking.CanMove(extrawindup) && !Orbwalking.CanAttack())
                        {
                            Q.Cast();
                        }
                    }
                    if (Q.IsReady() && Player.IsDashing())
                    {
                        Q.Cast();
                    }

                    if (Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) == 0 && !Player.HasBuff("rengarpassivebuff") && !Player.IsDashing())
                    {
                        var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                        if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                        {
                            E.Cast(targetE);
                        }
                        foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                        {
                            if (E.IsReady())
                                E.Cast(target);
                        }
                    }
                }
            }
            else if (mode == "Sadece Q")
            {
                if (Player.Mana < 5)
                {
                    if (Q.IsReady() && Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) != 0)
                    {
                        if (Orbwalking.CanMove(extrawindup) && !Orbwalking.CanAttack() /*&& dontwaitQ*/)
                        {
                            Q.Cast();
                        }
                    }
                    if (Orbwalking.CanMove(extrawindup))
                    {
                        var targetE = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                        if (E.IsReady() && targetE.IsValidTarget() && !targetE.IsZombie)
                        {
                            E.Cast(targetE);
                        }
                        foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(E.Range) && !x.IsZombie))
                        {
                            if (E.IsReady())
                                E.Cast(target);
                        }
                    }
                    var targetW = TargetSelector.GetTarget(500, TargetSelector.DamageType.Physical);
                    if (W.IsReady() && targetW.IsValidTarget() && !targetW.IsZombie)
                    {
                        W.Cast(targetW);
                    }
                }
                else
                {
                    if (Q.IsReady() && Player.CountEnemiesInRange(Player.AttackRange + Player.BoundingRadius + 100) != 0)
                    {
                        if (Orbwalking.CanMove(extrawindup) && !Orbwalking.CanAttack())
                        {
                            Q.Cast();
                        }
                    }
                    if (Q.IsReady() && Player.IsDashing())
                    {
                        Q.Cast();
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
                if (notifyselected.Text == "null")
                {
                    return;
                }
                else
                {
                    Notifications.RemoveNotification(notifyselected);
                    notifyselected = new Notification("null");
                    Notifications.AddNotification(notifyselected);
                }
            }
        }
        public static bool HasItem()
        {
            if (ItemData.Tiamat_Melee_Only.GetItem().IsReady() || ItemData.Ravenous_Hydra_Melee_Only.GetItem().IsReady())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static void CastItem()
        {

            if (ItemData.Tiamat_Melee_Only.GetItem().IsReady())
                ItemData.Tiamat_Melee_Only.GetItem().Cast();
            if (ItemData.Ravenous_Hydra_Melee_Only.GetItem().IsReady())
                ItemData.Ravenous_Hydra_Melee_Only.GetItem().Cast();
        }
        private static Notification notifyselected = new Notification("null");
    }
}
