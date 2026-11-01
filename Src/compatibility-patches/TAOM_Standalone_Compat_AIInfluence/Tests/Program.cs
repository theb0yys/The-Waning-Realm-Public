using System;
using System.IO;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using TaomAIInfluenceCompat;

internal static class Bootstrap
{
    internal static string Root;
    private static int Main(string[] args)
    {
        Root = args.Length > 0 ? args[0] : @"A:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord";
        AppDomain.CurrentDomain.AssemblyResolve += (s,e) => {
            string name = new AssemblyName(e.Name).Name;
            string primary = name == "TAOM" ? "Modules/TAOM/bin/Win64_Shipping_Client" :
                name == "AIInfluence" ? "Modules/AIInfluence/bin/Win64_Shipping_Client" :
                name == "SandBox" ? "Modules/SandBox/bin/Win64_Shipping_Client" :
                name == "0Harmony" ? "Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client" : "bin/Win64_Shipping_Client";
            foreach(string dir in new[] {primary, "Modules/TAOM.Dependencies/bin/Win64_Shipping_Client",
                "Modules/Bannerlord.Harmony/bin/Win64_Shipping_Client", "Modules/AIInfluence/bin/Win64_Shipping_Client"}) {
                string path = Path.Combine(Root, dir, name+".dll");
                if(File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        try { return (int)Assembly.GetExecutingAssembly().GetType("Regression").GetMethod("Run",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null); }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
internal static class Regression
{
    private static int passed;
    internal static int Run()
    {
        Assembly taom=Assembly.LoadFrom(Path.Combine(Bootstrap.Root,"Modules/TAOM/bin/Win64_Shipping_Client/TAOM.dll"));
        Assembly ai=Assembly.LoadFrom(Path.Combine(Bootstrap.Root,"Modules/AIInfluence/bin/Win64_Shipping_Client/AIInfluence.dll"));
        SubModule.RequireHash(taom,SubModule.TaomHash);
        SubModule.RequireHash(ai,SubModule.AIHash);
        Check(true,"current TAOM and unchanged AI Influence hashes accepted");
        Throws<InvalidOperationException>(()=>SubModule.RequireHash(taom,"7441F277EACC16EEEECF4C74A8859E89CB06C536EE83EBFB494F15F839A0D45C"),"old TAOM hash reproduces rejection");
        Throws<InvalidOperationException>(()=>SubModule.RequireHash(ai,SubModule.TaomHash),"wrong dependency hash rejected");
        MethodInfo bypassMethod=ai.GetType("AIInfluence.Diplomacy.DiplomacyPatches",true).GetMethod("WithBypass",new[]{typeof(Action)});
        Check(bypassMethod!=null && bypassMethod.IsStatic && bypassMethod.ReturnType==typeof(void),"installed WithBypass(Action) contract exists");
        var bypass=(Action<Action>)Delegate.CreateDelegate(typeof(Action<Action>),bypassMethod);
        int actions=0;
        bypass(()=>actions++);
        Check(actions==1,"real AI bypass invokes action once");
        Throws<InvalidOperationException>(()=>bypass(()=>{throw new InvalidOperationException("fixture");}),"real AI bypass preserves exceptions");
        bypass(()=>actions++);
        Check(actions==2,"real AI bypass remains callable after exception");
        var h=new Harmony("taom.compat.aiinfluence.diplomacy.v1");
        Type adapter=taom.GetType("TAOM.Adapters.AllianceAdapter",true);
        int wrapped=0;
        try {
            DiplomacyBridge.Install(h,adapter, action=>wrapped++);
            foreach(string name in new[]{"StartAlliance","DeclareWar","MakePeace"}) {
                MethodInfo method=adapter.GetMethod(name,new[]{typeof(string),typeof(string)});
                var patches=Harmony.GetPatchInfo(method);
                Check(patches!=null && patches.Transpilers.Count(p=>p.owner==h.Id)==1,name+" transpiler installed on real TAOM method");
                var calls=PatchProcessor.GetCurrentInstructions(method).Where(i=>i.operand is MethodInfo)
                    .Select(i=>(MethodInfo)i.operand).Count(m=>m.DeclaringType==typeof(DiplomacyBridge) && m.Name==name);
                Check(calls==1,name+" contains exactly one bridge call");
                MethodInfo wrapper=typeof(DiplomacyBridge).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);
                wrapper.Invoke(null,new object[wrapper.GetParameters().Length]);
            }
            Check(wrapped==3,"three native wrappers route through supplied bypass");
            Throws<InvalidOperationException>(()=>DiplomacyBridge.Install(h,adapter,bypass),"duplicate installation rejected");
            Throws<InvalidOperationException>(()=>DiplomacyBridge.Transpile(new CodeInstruction[0],typeof(Regression).GetMethod("Run",BindingFlags.Static|BindingFlags.NonPublic)),"unregistered method rejected");
            DiplomacyBridge.Uninstall(h);
            Check(new[]{"StartAlliance","DeclareWar","MakePeace"}.All(n=> {
                var p=Harmony.GetPatchInfo(adapter.GetMethod(n,new[]{typeof(string),typeof(string)}));
                return p==null || !p.Transpilers.Any(x=>x.owner==h.Id);
            }),"unload removes all owned transpilers");
            DiplomacyBridge.Install(h,adapter,bypass);
            Check(true,"bridge can reinstall with actual AI bypass");
        }
        finally { DiplomacyBridge.Uninstall(h); }
        Console.WriteLine("PASS: "+passed+" managed checks. Campaign/native diplomacy NOT_RUN.");
        return 0;
    }
    private static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);passed++;Console.WriteLine("PASS: "+name);}
    private static void Throws<T>(Action action,string name) where T:Exception {try{action();}catch(T){Check(true,name);return;}throw new Exception("FAIL: "+name);}
}
