using System;
using LeagueSharp;
using LeagueSharp.Common;
namespace Khazix
  {
    public class Program
      {
        private static Menu _config;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Spell _q, _w;
        private static void Main(string[] args)
          {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
          }
        private static void Game_OnGameLoad(EventArgs args)
          {
            if (ObjectManager.Player.ChampionName != "Khazix")
              return;
            _q = new Spell(SpellSlot.Q, 425);
            _w = new Spell(SpellSlot.W, 1000);
            _w.SetSkillshot(0.25f, 50, 1000, true, SkillshotType.SkillshotLine);
            _config = new Menu("Khazix", "Khazix", true);
            _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);
            _config.AddToMainMenu();
            Obj_AI_Base.OnProcessSpellCast += oncast;
            Game.OnUpdate += Game_OnUpdate;
          }
        private static void oncast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
          {
            var spell = args.SData;
            if (!sender.IsMe)
              {
                return;
              }
            if (spell.Name.ToLower().Contains("khazixq") || spell.Name.ToLower().Contains("khazixqlong") || spell.Name.ToLower().Contains("khazixw") || spell.Name.ToLower().Contains("khazixwlong") || spell.Name.ToLower().Contains("khazixe") || spell.Name.ToLower().Contains("khazixelong") || spell.Name.ToLower().Contains("khazixr"))
              {
                Utility.DelayAction.Add(450, Orbwalking.ResetAutoAttackTimer);
              }
          }
        private static void Game_OnUpdate(EventArgs args)
          {
            var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
              {
                if (target != null)
                  {
                    if (target.IsValidTarget(300))
                      {
                        Items.UseItem(3074);
                        Items.UseItem(3077);
                        Items.UseItem(3142);
                        Items.UseItem(3143);
                      }
                    if (_w.IsReady() && target.IsValidTarget(_w.Range))
                      {
                        var WPred = _w.GetPrediction(target);
                        if (WPred.Hitchance >= HitChance.High)
                          {
                            _w.Cast(WPred.CastPosition);
                          }
                      }
                    if (_q.IsReady() && target.IsValidTarget(_q.Range))
                      {
                        _q.CastOnUnit(target);
                      }
                  }
              }
          }
      }
  }