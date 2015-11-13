using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.Drawing;
namespace Rengar
{
public class Program
{
#region declarations
  private static Menu _config;
  private static Orbwalking.Orbwalker _orbwalker;
  private static Spell _q;
  private static Spell _w;
  private static Spell _e;
  private static int _lastTick;
#endregion
#region Main
  private static void Main(string[] args)
    {
      CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
    }
#endregion
#region OnGameLoad
  private static void Game_OnGameLoad(EventArgs args)
    {
      if (ObjectManager.Player.ChampionName != "Rengar")
        return;
      _q = new Spell(SpellSlot.Q, 230);
      _w = new Spell(SpellSlot.W, 400);
      _e = new Spell(SpellSlot.E, 1000);
      _e.SetSkillshot(0.25f, 70, 1500, true, SkillshotType.SkillshotLine);
      _e.MinHitChance = HitChance.Medium;
      _config = new Menu("Rengar", "Rengar", true);
      _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));
      var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
      TargetSelector.AddToMenu(targetSelectorMenu);
      _config.AddSubMenu(targetSelectorMenu);
      _config.SubMenu("Combo").AddItem(new MenuItem("ComboSwitch", "Combo switch Key").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
      _config.SubMenu("Combo").AddItem(new MenuItem("ComboMode", "Combo Mode").SetValue(new StringList(new[]{"Empowered Q", "Empowered E"})));
      _config.SubMenu("Combo").AddItem(new MenuItem("orbswitch", "Orbwalk switch Key").SetValue(new KeyBind("U".ToCharArray()[0], KeyBindType.Press)));
      _config.SubMenu("Combo").AddItem(new MenuItem("orbmode", "Orbwalking Mode").SetValue(new StringList(new[]{"MIXED ORBWALKING", "ONESHOT ORBWALKING"})));
      _config.SubMenu("Settings ON/OFF").AddItem(new MenuItem("eq", "use E in Q mode if target out of range").SetValue(true));
      _config.SubMenu("Settings ON/OFF").SubMenu("Drawings").AddItem(new MenuItem("orbd", "draw orb mode text").SetValue(true));
      _config.SubMenu("Settings ON/OFF").SubMenu("Drawings").AddItem(new MenuItem("empd", "draw empowered mode text").SetValue(true));
      _config.SubMenu("Settings ON/OFF").SubMenu("Drawings").AddItem(new MenuItem("drawtar", "Active Enemy").SetValue(new Circle(true, Color.GreenYellow)));
      _config.SubMenu("Autoheal").AddItem(new MenuItem("autoheal", "%hp autoheal").SetValue(new Slider(33, 100, 0)));
      _config.AddToMainMenu();
      Drawing.OnDraw += Drawing_OnDraw;
      Obj_AI_Base.OnProcessSpellCast += oncast;
      Game.OnUpdate += Game_OnUpdate;
    }
#endregion
#region resetQ
private static void oncast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
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
#endregion
#region OnGameUpdate
private static void Game_OnUpdate(EventArgs args)
{
  var hp = _config.Item("autoheal").GetValue<Slider>().Value;
  if ((ObjectManager.Player.Mana == 5) && ((ObjectManager.Player.Health/ObjectManager.Player.MaxHealth)*100 <= hp))
    {
      _w.Cast();
    }
  ComboModeSwitch();
  OrbModeSwitch();  
  var searchtarget = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);
  var closetarget = TargetSelector.GetTarget(350, TargetSelector.DamageType.Physical);
  var orbmod = _config.SubMenu("Combo").Item("orbmode").GetValue<StringList>().SelectedIndex;
  if (TargetSelector.GetPriority(searchtarget) == 2.5f)
    {
      if (searchtarget.IsValidTarget(1500) && (ObjectManager.Player.HasBuff("rengarpassivebuff") || ObjectManager.Player.HasBuff("rengarbushspeedbuff") || ObjectManager.Player.HasBuff("rengarr")))
        {
          TargetSelector.SetTarget(searchtarget);
          _config.Item("ForceFocusSelected").SetValue(true);
        }
    }
  if (_config.Item("ForceFocusSelected").GetValue<bool>() && !ObjectManager.Player.HasBuff("rengarpassivebuff") && !ObjectManager.Player.HasBuff("rengarbushspeedbuff") && !ObjectManager.Player.HasBuff("rengarr"))
    {
      _config.Item("ForceFocusSelected").SetValue(false);
    }
  if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
    {
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
              _q.Cast();
            }
          if (searchtarget.IsValidTarget(800) && (ObjectManager.Player.HasBuff("rengarpassivebuff") || ObjectManager.Player.HasBuff("rengarbushspeedbuff") || ObjectManager.Player.HasBuff("rengarr")))
            {
              Items.UseItem(3142);
            }
          if (searchtarget.IsValidTarget(1000) && !ObjectManager.Player.HasBuff("rengarpassivebuff") && !ObjectManager.Player.HasBuff("rengarbushspeedbuff") && !ObjectManager.Player.HasBuff("rengarr"))
            {
              _e.Cast(searchtarget);
                if (orbmod == 0)
                  {
                    if ((_q.IsReady() || _w.IsReady() || _e.IsReady()) && closetarget.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 100)
                      {
                        ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, closetarget);
                      }
                  }
                if (orbmod == 1)
                  {
                    if (closetarget.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 100)
                      {
                        ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, closetarget);
                      }
                  }
            }
          if (closetarget.IsValidTarget(_q.Range))
            {
              _q.Cast(closetarget);
            }
          if (closetarget.IsValidTarget(350))
            {
              _e.Cast(closetarget);
              _w.Cast(closetarget);
                if (orbmod == 0)
                  {
                    if ((_q.IsReady() || _w.IsReady() || _e.IsReady()) && closetarget.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 100)
                      {
                        ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, closetarget);
                      }
                  }
                if (orbmod == 1)
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
          var comboMode = _config.SubMenu("Combo").Item("ComboMode").GetValue<StringList>().SelectedIndex;
          var einq = _config.Item("eq").GetValue<bool>();
          switch (comboMode)
            {
              case 0:
                      {
                        if ((ObjectManager.Player.Health/ObjectManager.Player.MaxHealth)*100 <= hp)
                          {
                            _w.Cast();
                          }
                        if (searchtarget.IsValidTarget(1000) && (ObjectManager.Player.HasBuff("rengarpassivebuff") || ObjectManager.Player.HasBuff("rengarbushspeedbuff") || ObjectManager.Player.HasBuff("rengarr")) && (ObjectManager.Player.Health/ObjectManager.Player.MaxHealth)*100 > hp)
                          {
                            _q.Cast();
                          }
                        if (searchtarget.IsValidTarget(800) && (ObjectManager.Player.HasBuff("rengarpassivebuff") || ObjectManager.Player.HasBuff("rengarbushspeedbuff") || ObjectManager.Player.HasBuff("rengarr")))
                          {
                            Items.UseItem(3142);
                          }
                        if (closetarget.IsValidTarget(_q.Range) && (ObjectManager.Player.Health/ObjectManager.Player.MaxHealth)*100 > hp)
                          {
                            _q.Cast(closetarget);
                          }
                        if (einq && searchtarget.Distance(ObjectManager.Player.Position) > 250 && searchtarget.Distance(ObjectManager.Player.Position) < 1000 && !ObjectManager.Player.HasBuff("rengarpassivebuff") && !ObjectManager.Player.HasBuff("rengarbushspeedbuff") && !ObjectManager.Player.HasBuff("rengarr") && (ObjectManager.Player.Health/ObjectManager.Player.MaxHealth)*100 > hp)
                          {
                            _e.Cast(searchtarget);
                          }
                        if (orbmod == 0)
                          {
                            if ((_q.IsReady() || _w.IsReady() || _e.IsReady()) && closetarget.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 100)
                              {
                                ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, closetarget);
                              }
                          }
                        if (orbmod == 1)
                          {
                            if (closetarget.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 100)
                              {
                                ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, closetarget);
                              }
                          }
                      }
              break;
              case 1:
                      {
                        if ((ObjectManager.Player.Health/ObjectManager.Player.MaxHealth)*100 <= hp)
                          {
                            _w.Cast();
                          }
                        if (searchtarget.IsValidTarget(800) && (ObjectManager.Player.HasBuff("rengarpassivebuff") || ObjectManager.Player.HasBuff("rengarbushspeedbuff") || ObjectManager.Player.HasBuff("rengarr")))
                          {
                            Items.UseItem(3142);
                          }
                        if (searchtarget.IsValidTarget(1000) && !ObjectManager.Player.HasBuff("rengarpassivebuff") && !ObjectManager.Player.HasBuff("rengarbushspeedbuff") && !ObjectManager.Player.HasBuff("rengarr") && (ObjectManager.Player.Health/ObjectManager.Player.MaxHealth)*100 > hp)
                          {
                            _e.Cast(searchtarget);
                          }
                        if (closetarget.IsValidTarget(350) && (ObjectManager.Player.Health/ObjectManager.Player.MaxHealth)*100 > hp)
                          {
                            _e.Cast(closetarget);
                          }
                        if (orbmod == 0)
                          {
                            if ((_q.IsReady() || _w.IsReady() || _e.IsReady()) && closetarget.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 100)
                              {
                                ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, closetarget);
                              }
                          }
                        if (orbmod == 1)
                          {
                            if (closetarget.Distance(ObjectManager.Player) < Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) + 100)
                              {
                                ObjectManager.Player.IssueOrder(GameObjectOrder.AttackUnit, closetarget);
                              }
                          }
                      }
              break;
            }
        }
    }
}
#endregion
#region empswitch
private static void ComboModeSwitch()
{
  var comboMode = _config.SubMenu("Combo").Item("ComboMode").GetValue<StringList>().SelectedIndex;
  var lasttime = Environment.TickCount - _lastTick;
    if (!_config.SubMenu("Combo").Item("ComboSwitch").GetValue<KeyBind>().Active || lasttime <= Game.Ping)
      {
        return;
      }
    switch (comboMode)
      {
        case 0:
          _config.SubMenu("Combo").Item("ComboMode").SetValue(new StringList(new[]{"Empowered Q", "Empowered E"}, 1));
          _lastTick = Environment.TickCount + 300;
        break;
        case 1:
          _config.SubMenu("Combo").Item("ComboMode").SetValue(new StringList(new[]{"Empowered Q", "Empowered E"}));
          _lastTick = Environment.TickCount + 300;
        break;
      }
}
#endregion
#region orbswitch
private static void OrbModeSwitch()
{
  var orbMode = _config.SubMenu("Combo").Item("orbmode").GetValue<StringList>().SelectedIndex;
  var lasttime = Environment.TickCount - _lastTick;
    if (!_config.SubMenu("Combo").Item("orbswitch").GetValue<KeyBind>().Active || lasttime <= Game.Ping)
      {
        return;
      }
    switch (orbMode)
      {
        case 0:
          _config.SubMenu("Combo").Item("orbmode").SetValue(new StringList(new[]{"MIXED ORBWALKING", "ONESHOT ORBWALKING"}, 1));
          _lastTick = Environment.TickCount + 300;
        break;
        case 1:
          _config.SubMenu("Combo").Item("orbmode").SetValue(new StringList(new[]{"MIXED ORBWALKING", "ONESHOT ORBWALKING"}));
          _lastTick = Environment.TickCount + 300;
        break;
      }
}
#endregion
#region draw
private static void Drawing_OnDraw(EventArgs args)
{
  var wts = Drawing.WorldToScreen(ObjectManager.Player.Position);
  var emp = _config.SubMenu("Combo").Item("ComboMode").GetValue<StringList>().SelectedIndex;
  var orbmode = _config.SubMenu("Combo").Item("orbmode").GetValue<StringList>().SelectedIndex;
  var empd = _config.Item("empd").GetValue<bool>();
  var orbd = _config.Item("orbd").GetValue<bool>();
  var drawt = _config.Item("drawtar").GetValue<Circle>();
  var searchtarget = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Physical);
  var closetarget = TargetSelector.GetTarget(350, TargetSelector.DamageType.Physical);
  if (drawt.Active)
    {
      if (searchtarget.IsValidTarget(1500))
        {
          Render.Circle.DrawCircle(searchtarget.Position, 115f, drawt.Color, 1);
        }
      if (closetarget.IsValidTarget(350))
        {
          Render.Circle.DrawCircle(closetarget.Position, 115f, drawt.Color, 1);
        }
    }    
  if (empd)
    {
      switch (emp)
        {
          case 0:
            Drawing.DrawText(wts[0], wts[1], Color.White, "Q");
          break;
          case 1:
            Drawing.DrawText(wts[0], wts[1], Color.White, "E");
          break;
        }
    }
  if (orbd)
    {
      switch (orbmode)
        {
          case 0:
            Drawing.DrawText(wts[0] - 70, wts[1] + 25, Color.White, "MIXED ORBWALKING");
          break;
          case 1:
            Drawing.DrawText(wts[0] - 70, wts[1] + 25, Color.White, "ONESHOT ORBWALKING");
          break;
        }
    }
}
#endregion
}
}