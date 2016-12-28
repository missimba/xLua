﻿using UnityEngine;
using System.Collections;
using XLua;

[CSharpCallLua]
public delegate int TestOutDelegate(HotfixCalc calc, int a, out double b, ref string c);

[Hotfix]
public class HotfixCalc
{
    public int Add(int a, int b)
    {
        return a - b;
    }

    public Vector3 Add(Vector3 a, Vector3 b)
    {
        return a - b;
    }

    public int TestOut(int a, out double b, ref string c)
    {
        b = a + 2;
        c = "wrong version";
        return a + 3;
    }
}

public class NoHotfixCalc
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}


public class HotfixTest2 : MonoBehaviour {

	// Use this for initialization
	void Start () {
        LuaEnv luaenv = new LuaEnv();
        HotfixCalc calc = new HotfixCalc();
        NoHotfixCalc ordinaryCalc = new NoHotfixCalc();

        int CALL_TIME = 100 * 1000 * 1000 ;
        var start = System.DateTime.Now;
        for (int i = 0; i < CALL_TIME; i++)
        {
            calc.Add(2, 1);
        }
        var d1 = (System.DateTime.Now - start).TotalMilliseconds;
        Debug.Log("Hotfix using:" + d1);

        start = System.DateTime.Now;
        for (int i = 0; i < CALL_TIME; i++)
        {
            ordinaryCalc.Add(2, 1);
        }
        var d2 = (System.DateTime.Now - start).TotalMilliseconds;
        Debug.Log("No Hotfix using:" + d2);

        Debug.Log("drop:" + ((d1 - d2) / d1));

        Debug.Log("Before Fix: 2 + 1 = " + calc.Add(2, 1));
        Debug.Log("Before Fix: Vector3(2, 3, 4) + Vector3(1, 2, 3) = " + calc.Add(new Vector3(2, 3, 4), new Vector3(1, 2, 3)));
        luaenv.DoString(@"
            xlua.hotfix(CS.HotfixCalc, 'Add', function(self, a, b)
                return a + b
            end)
        ");
        Debug.Log("After Fix: 2 + 1 = " + calc.Add(2, 1));
        Debug.Log("After Fix: Vector3(2, 3, 4) + Vector3(1, 2, 3) = " + calc.Add(new Vector3(2, 3, 4), new Vector3(1, 2, 3)));

        double num;
        string str = "hehe";
        int ret = calc.TestOut(100, out num, ref str);
        Debug.Log("ret = " + ret + ", num = " + num + ", str = " + str);

        luaenv.DoString(@"
            xlua.hotfix(CS.HotfixCalc, 'TestOut', function(self, a, c)
                    print('TestOut', self, a, c)
                    return a + 10, a + 20, 'right version'
                end)
        ");
        str = "hehe";
        ret = calc.TestOut(100, out num, ref str);
        Debug.Log("ret = " + ret + ", num = " + num + ", str = " + str);

        Debug.Log("----------------------stateful------------------------");
        Debug.Log("----------------------before------------------------");
        TestStateful();
        luaenv.DoString(@"
            xlua.hotfix(CS.StatefullTest, {
                XLuaConstructor = function(csobj)
                    return {evt = {}, start = 0}
                end;
                set_AProp = function(self, v)
                    print('set_AProp', v)
                    self.AProp = v
                end;
                get_AProp = function(self)
                    return self.AProp
                end;
                get_Item = function(self, k)
                    print('get_Item', k)
                    return 1024
                end;
                set_Item = function(self, k, v)
                    print('set_Item', k, v)
                end;
                add_AEvent = function(self, cb)
                    print('add_AEvent', cb)
                    table.insert(self.evt, cb)
                end;
                remove_AEvent = function(self, cb)
                   print('remove_AEvent', cb)
                   for i, v in ipairs(self.evt) do
                       if v == cb then
                           table.remove(self.evt, i)
                           break
                       end
                   end
                end;
                Start = function(self)
                    print('Start')
                    for _, cb in ipairs(self.evt) do
                        cb(self.start, 2)
                    end
                    self.start = self.start + 1
                end;
                StaticFunc = function(a, b, c)
                   print(a, b, c)
                end
           })
        ");
        Debug.Log("----------------------after------------------------");
        TestStateful();
    }

    void TestStateful()
    {
        StatefullTest sft = new StatefullTest();
        sft.AProp = 10;
        Debug.Log("sft.AProp:" + sft.AProp);
        sft["1"] = 1;
        Debug.Log("sft['1']:" + sft["1"]);
        System.Action<int, double> cb = (a, b) =>
        {
            Debug.Log("a:" + a + ",b:" + b);
        };
        sft.AEvent += cb;
        sft.Start();
        sft.Start();
        sft.AEvent -= cb;
        sft.Start();
        StatefullTest.StaticFunc(1, 2);
        StatefullTest.StaticFunc("e", 3, 4);
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}