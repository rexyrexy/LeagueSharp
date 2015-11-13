using System;
using LeagueSharp;
using LeagueSharp.Common;
namespace Irelia
{
  public class Program
    {
      private static Menu _config;
      private static Orbwalking.Orbwalker _orbwalker;
      private static Spell _q, _w, _e, _r;
      private static void Main(string[] args)
        {
          CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
      private static void Game_OnGameLoad(EventArgs args)
        {
          if (ObjectManager.Player.ChampionName != "Irelia")
            return;
          _q = new Spell(SpellSlot.Q, 900);
          _w = new Spell(SpellSlot.W, 250);
          _e = new Spell(SpellSlot.E, 425);
          _r = new Spell(SpellSlot.R, 1000);
          _r.SetSkillshot(0.25f, 70, 1200, false, SkillshotType.SkillshotLine);
          _config = new Menu("Irelia", "Irelia", true);
          _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));
          var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
          TargetSelector.AddToMenu(targetSelectorMenu);
          _config.AddSubMenu(targetSelectorMenu);
          _config.AddToMainMenu();
          Obj_AI_Base.OnProcessSpellCast += oncast;
          Game.OnUpdate += Game_OnUpdate;
        }
      private static void Game_OnUpdate(EventArgs args)
        {
          if (_r.Level <= 2)
            {
              ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.R);
            }
          var target = TargetSelector.GetTarget(1200, TargetSelector.DamageType.Physical);
          var tw = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Physical);
          var te = TargetSelector.GetTarget(_e.Range, TargetSelector.DamageType.Magical);
          if (te != null)
            {
              if (_e.IsReady() && te.IsValidTarget(_e.Range) && (te.Health/te.MaxHealth)*100 > (ObjectManager.Player.Health/ObjectManager.Player.MaxHealth)*100)
                {
                  _e.CastOnUnit(te);
                }
            }
          if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
              if (tw != null)
                {
                  if (_w.IsReady() && target.IsValidTarget(_w.Range))
                    {
                      _w.Cast();
                    }
                }
              if (target != null)
                {
                  if (_q.IsReady() && _w.IsReady() && target.IsValidTarget(_q.Range))
                    {
                      _w.Cast();
                    }
                  if (_q.IsReady() && target.IsValidTarget(_q.Range) && target.Distance(ObjectManager.Player.Position) > Orbwalking.GetRealAutoAttackRange(ObjectManager.Player))
                    {
                      _q.CastOnUnit(target);
                    }
                  else if (_e.IsReady() && target.IsValidTarget(_e.Range))
                    {
                      _e.CastOnUnit(target);
                    }
                  else if (_r.IsReady() && target.IsValidTarget(_r.Range))
                    {
                      var RPred = _r.GetPrediction(target);
                      if (RPred.Hitchance >= HitChance.High)
                        {
                          _r.Cast(RPred.CastPosition);
                        }
                    }
                  if (target.IsValidTarget(500))
                    {
                      Items.UseItem(3144, target);
                      Items.UseItem(3146, target);
                      Items.UseItem(3153, target);
                    }
                  if (target.IsValidTarget(350))
                    {
                      Items.UseItem(3074);
                      Items.UseItem(3077);
                      Items.UseItem(3142);
                      Items.UseItem(3143);
                    }
                }
            }
        }
      private static void oncast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
          var spell = args.SData;
          if (!sender.IsMe)
            {
              return;
            }
          if (spell.Name.ToLower().Contains("ireliagatotsu") || spell.Name.ToLower().Contains("ireliahitenstyle") || spell.Name.ToLower().Contains("ireliaequilibriumstrike") || spell.Name.ToLower().Contains("ireliatranscendentblades"))
            {
              Utility.DelayAction.Add(450, Orbwalking.ResetAutoAttackTimer);
            }
        }
    }
}