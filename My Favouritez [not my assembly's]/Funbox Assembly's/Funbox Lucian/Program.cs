using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
namespace Lucian
  {
    public class Program
      {
        private static Menu _config;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Spell _q, _q2, _w, _w2;
        private static string[] select = {"Ashe", "Caitlyn", "Corki", "Draven", "Ezreal", "Graves", "Jinx", "Kalista", "KogMaw", "Lucian", "MissFortune","Quinn","Sivir","Teemo","Tristana","TwistedFate","Twitch","Urgot","Varus","Vayne"};
        private static void Main(string[] args)
          {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
          }
        private static void Game_OnGameLoad(EventArgs args)
          {
            if (ObjectManager.Player.ChampionName != "Lucian")
              return;
            _q = new Spell(SpellSlot.Q, 700);
            _q2 = new Spell(SpellSlot.Q, 1200);
            _w = new Spell(SpellSlot.W, 700);
            _w2 = new Spell(SpellSlot.W, 1100);
            _q2.SetSkillshot(0.25f, 70, 3000, false, SkillshotType.SkillshotLine);
            _w.SetSkillshot(0.25f, 70, 1500, false, SkillshotType.SkillshotLine);
            _w2.SetSkillshot(0.25f, 70, 1500, true, SkillshotType.SkillshotLine);
            _config = new Menu("Lucian", "Lucian", true);
            _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);
            _config.AddItem(new MenuItem("qexk", "Q ex in combo only if killable").SetValue(true));
            _config.AddItem(new MenuItem("qexk2", "Q ex in combo only selected champion in harass menu").SetValue(true));
            foreach (var hero in HeroManager.Enemies) 
              {
                _config.SubMenu("Harass").AddItem(new MenuItem("auto" + hero.ChampionName, hero.ChampionName).SetValue(select.Contains(hero.ChampionName)));
              }
            _config.SubMenu("Harass").AddItem(new MenuItem("manah", "%mana").SetValue(new Slider(33, 100, 0)));
            _config.AddToMainMenu();
            Obj_AI_Base.OnProcessSpellCast += oncast;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            Game.OnUpdate += Game_OnUpdate;
          }
        private static void Game_OnUpdate(EventArgs args)
          {
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range, MinionTypes.All, MinionTeam.NotAlly);
            var tex = TargetSelector.GetTarget(1200, TargetSelector.DamageType.Physical);
            var targetqe = HeroManager.Enemies.Where(hero => hero.IsValidTarget(_q2.Range)).FirstOrDefault(hero => _config.Item("auto" + hero.ChampionName).GetValue<bool>());
            var targetqkk = HeroManager.Enemies.Where(hero => hero.IsValidTarget(_q2.Range)).Where(hero => hero.Distance(ObjectManager.Player) > _q.Range).FirstOrDefault(hero => _config.Item("auto" + hero.ChampionName).GetValue<bool>());
            var targetqk = HeroManager.Enemies.Where(hero => hero.IsValidTarget(_q2.Range)).Where(hero => hero.Distance(ObjectManager.Player) > _q.Range).FirstOrDefault(hero => ObjectManager.Player.GetSpellDamage(hero, SpellSlot.Q) >= hero.Health);
            if (_w2.IsReady())
              {
                foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(_w2.Range)))
                  {
                    if (ObjectManager.Player.GetSpellDamage(target, SpellSlot.W) >= target.Health)
                      {
                        var WPred = _w2.GetPrediction(target);
                        if (WPred.Hitchance >= HitChance.High)
                          {
                            _w2.Cast(WPred.CastPosition);
                          }
                      }
                  }
              }
            if (_q2.IsReady())
              {
                if (!_config.Item("qexk2").GetValue<bool>())
                  {
                    if (targetqk != null)
                      {
                        foreach (var minion in minions)
                          {
                            if (_q.IsReady() && _q2.WillHit(targetqk, ObjectManager.Player.ServerPosition.Extend(minion.ServerPosition, _q2.Range), 0, HitChance.VeryHigh))
                              {
                                _q2.CastOnUnit(minion);
                              }
                          }
                      }
                  }
                if (_config.Item("qexk2").GetValue<bool>())
                  {
                    if (targetqkk != null && ObjectManager.Player.GetSpellDamage(targetqkk, SpellSlot.Q) >= targetqkk.Health)
                      {
                        foreach (var minion in minions)
                          {
                            if (_q.IsReady() && _q2.WillHit(targetqkk, ObjectManager.Player.ServerPosition.Extend(minion.ServerPosition, _q2.Range), 0, HitChance.VeryHigh))
                              {
                                _q2.CastOnUnit(minion);
                              }
                          }
                      }
                  }
              }
            if (_q.IsReady())
              {
                foreach (var target in HeroManager.Enemies.Where(x => x.IsValidTarget(_q.Range)))
                  {
                    if (ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) >= target.Health)
                      {
                        _q.CastOnUnit(target);
                      }
                  }
              }
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear || _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed || _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
              {
                if (targetqe != null)
                  {
                    if ((ObjectManager.Player.Mana/ObjectManager.Player.MaxMana)*100 > _config.Item("manah").GetValue<Slider>().Value)
                      {
                        foreach (var minion in minions)
                          {
                            if (_q.IsReady() && _q2.WillHit(targetqe, ObjectManager.Player.ServerPosition.Extend(minion.ServerPosition, _q2.Range), 0, HitChance.VeryHigh))
                              {
                                _q2.CastOnUnit(minion);
                              }
                          }
                      }
                  }
              }
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
              {
                if (!_config.Item("qexk").GetValue<bool>() && !_config.Item("qexk2").GetValue<bool>())
                  {
                    if (tex != null && tex.Distance(ObjectManager.Player) > _q.Range)
                      {
                        foreach (var minion in minions)
                          {
                            if (_q.IsReady() && _q2.WillHit(tex, ObjectManager.Player.ServerPosition.Extend(minion.ServerPosition, _q2.Range), 0, HitChance.VeryHigh))
                              {
                                _q2.CastOnUnit(minion);
                              }
                          }
                      }
                  }
                if (!_config.Item("qexk").GetValue<bool>() && _config.Item("qexk2").GetValue<bool>())
                  {
                    if (targetqe != null && targetqe.Distance(ObjectManager.Player) > _q.Range)
                      {
                        foreach (var minion in minions)
                          {
                            if (_q.IsReady() && _q2.WillHit(targetqe, ObjectManager.Player.ServerPosition.Extend(minion.ServerPosition, _q2.Range), 0, HitChance.VeryHigh))
                              {
                                _q2.CastOnUnit(minion);
                              }
                          }
                      }
                  }
              }
          }
        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
          {
            var tq = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Physical);
            var tw = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Magical);
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
              {
                if (tq != null)
                  {
                    if (_q.IsReady() && tq.IsValidTarget(_q.Range))
                      {
                        _q.CastOnUnit(tq);
                      }
                    if (tq.IsValidTarget(550))
                      {
                        Items.UseItem(3144, tq);
                        Items.UseItem(3153, tq);
                      }
                  }
                if (tw != null)
                  {
                    if (!_q.IsReady() && _w.IsReady() && tw.IsValidTarget(_w.Range))
                      {
                        var WPred = _w.GetPrediction(tw);
                        if (WPred.Hitchance >= HitChance.Low)
                          {
                            _w.Cast(WPred.CastPosition);
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
            if (spell.Name.ToLower().Contains("lucianq") || spell.Name.ToLower().Contains("lucianw") || spell.Name.ToLower().Contains("luciane"))
              {
                Utility.DelayAction.Add(450, Orbwalking.ResetAutoAttackTimer);
              }
          }
      }
  }