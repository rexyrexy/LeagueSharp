using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
namespace Diana
{
  public class Program
    {
      private static Menu _config;
      private static Orbwalking.Orbwalker _orbwalker;
      private static Spell _q, _q2, _w, _e, _r;
      private static void Main(string[] args)
        {
          CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
      private static void Game_OnGameLoad(EventArgs args)
        {
          if (ObjectManager.Player.ChampionName != "Diana")
            return;
          _q = new Spell(SpellSlot.Q, 830);
          _q2 = new Spell(SpellSlot.Q, 350);
          _w = new Spell(SpellSlot.W, 200);
          _e = new Spell(SpellSlot.E, 350);
          _r = new Spell(SpellSlot.R, 825);
          _q.SetSkillshot(0.25f, 90, 900, false, SkillshotType.SkillshotCircle);
          _q2.SetSkillshot(0.25f, 60, 2500, false, SkillshotType.SkillshotCircle);
          _config = new Menu("Diana", "Diana", true);
          _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));
          var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
          TargetSelector.AddToMenu(targetSelectorMenu);
          _config.AddSubMenu(targetSelectorMenu);
          _config.AddItem(new MenuItem("ec", "E mode").SetValue(new StringList(new[]{"after W", "in mid jump", "E off"})));
          _config.SubMenu("autokill").AddItem(new MenuItem("autokil", "autokill").SetValue(true));
          _config.SubMenu("autokill").AddItem(new MenuItem("kill", "autokill only if <= x enemys in 1600 range").SetValue(new Slider(5, 5, 1)));
          foreach (var hero in HeroManager.Enemies)
            {
              _config.SubMenu("autokill").SubMenu("autokill champion select").AddItem(new MenuItem("auto" + hero.ChampionName, hero.ChampionName).SetValue(false));
            }
          _config.AddToMainMenu();
          Obj_AI_Base.OnProcessSpellCast += oncast;
          Game.OnUpdate += Game_OnUpdate;
        }
      private static void Game_OnUpdate(EventArgs args)
        {
          var target = HeroManager.Enemies.Where(hero => hero.IsValidTarget(1200)).FirstOrDefault(hero => _config.Item("auto" + hero.ChampionName).GetValue<bool>());
          if (target != null && ObjectManager.Player.CountEnemiesInRange(1600) <= _config.Item("kill").GetValue<Slider>().Value)
            {
              if (_config.SubMenu("autokill").Item("autokil").GetValue<bool>())
                {
                  if (_q.IsReady() && _w.IsReady() && _r.IsReady() && (ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) + ObjectManager.Player.GetSpellDamage(target, SpellSlot.W) + ObjectManager.Player.GetSpellDamage(target, SpellSlot.R)) > target.Health)
                    {
                      var QPred = _q.GetPrediction(target);
                      var Q2Pred = _q2.GetPrediction(target);
                      if (target.Distance(ObjectManager.Player) > _q2.Range && QPred.Hitchance >= HitChance.High)
                        {
                          _q.Cast(QPred.CastPosition);
                        }
                      if (target.Distance(ObjectManager.Player) <= _q2.Range && Q2Pred.Hitchance >= HitChance.High)
                        {
                          _q2.Cast(Q2Pred.CastPosition);
                        }
                      if (target.HasBuff("dianamoonlight"))
                        {
                          _r.CastOnUnit(target);
                        }
                      if (!_r.IsReady())
                        {
                          _w.Cast();
                        }
                    }
                  if (_q.IsReady() && !_w.IsReady() && _r.IsReady() && (ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) + ObjectManager.Player.GetSpellDamage(target, SpellSlot.R)) > target.Health)
                    {
                      var QPred = _q.GetPrediction(target);
                      var Q2Pred = _q2.GetPrediction(target);
                      if (target.Distance(ObjectManager.Player) > _q2.Range && QPred.Hitchance >= HitChance.High)
                        {
                          _q.Cast(QPred.CastPosition);
                        }
                      if (target.Distance(ObjectManager.Player) <= _q2.Range && Q2Pred.Hitchance >= HitChance.High)
                        {
                          _q2.Cast(Q2Pred.CastPosition);
                        }
                      if (target.HasBuff("dianamoonlight"))
                        {
                          _r.CastOnUnit(target);
                        }
                    }
                  if (!_q.IsReady() && _w.IsReady() && _r.IsReady() && (ObjectManager.Player.GetSpellDamage(target, SpellSlot.W) + ObjectManager.Player.GetSpellDamage(target, SpellSlot.R)) > target.Health)
                    {
                      _r.CastOnUnit(target);
                      if (!_r.IsReady())
                        {
                          _w.Cast();
                        }
                    }
                  if (!_q.IsReady() && !_w.IsReady() && _r.IsReady() && ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) > target.Health)
                    {
                      _r.CastOnUnit(target);
                    }
                  if (_q.IsReady() && !_r.IsReady() && ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) > target.Health)
                    {
                      var QPred = _q.GetPrediction(target);
                      var Q2Pred = _q2.GetPrediction(target);
                      if (target.Distance(ObjectManager.Player) > _q2.Range && QPred.Hitchance >= HitChance.High)
                        {
                          _q.Cast(QPred.CastPosition);
                        }
                      if (target.Distance(ObjectManager.Player) <= _q2.Range && Q2Pred.Hitchance >= HitChance.High)
                        {
                          _q2.Cast(Q2Pred.CastPosition);
                        }
                    }
                }
            }
          if (_r.Level <= 2)
            {
              ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.R);
            }
          if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
              var tt = TargetSelector.GetTarget(900, TargetSelector.DamageType.Magical);
              if (tt != null)
                {
                  var QPred = _q.GetPrediction(target);
                  var Q2Pred = _q2.GetPrediction(target);
                  if (target.Distance(ObjectManager.Player) > _q2.Range && QPred.Hitchance >= HitChance.High)
                    {
                      _q.Cast(QPred.CastPosition);
                    }
                  if (target.Distance(ObjectManager.Player) <= _q2.Range && Q2Pred.Hitchance >= HitChance.High)
                    {
                      _q2.Cast(Q2Pred.CastPosition);
                    }
                  if (_w.IsReady() && tt.IsValidTarget(_w.Range))
                    {
                      _w.Cast();
                    }
                  if (_e.IsReady() && tt.IsValidTarget(_e.Range))
                    {
                      var emode = _config.Item("ec").GetValue<StringList>().SelectedIndex;
                      switch (emode)
                        {
                          case 0:
                            if (!_w.IsReady())
                              {
                                _e.Cast();
                              }
                          break;
                          case 1:
                            _e.Cast();
                          break;
                        }
                    }
                  if (tt.HasBuff("dianamoonlight"))
                    {
                      if (_r.IsReady() && tt.IsValidTarget(_r.Range))
                        {
                          _r.CastOnUnit(tt);
                        }
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
          if (spell.Name.ToLower().Contains("dianaarc") || spell.Name.ToLower().Contains("dianateleport"))
            {
              Utility.DelayAction.Add(450, Orbwalking.ResetAutoAttackTimer);
            }
        }
    }
}