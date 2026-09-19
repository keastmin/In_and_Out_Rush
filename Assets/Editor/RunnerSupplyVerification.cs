using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using KIM.Dev;
using ProjectIO.RunnerSupply;

[InitializeOnLoad]
public static class RunnerSupplyVerification
{
    private const string Work="Library/RunnerSupplyTools";
    private const string TestKey="RunnerSupplyVerification.PlayTest";
    static RunnerSupplyVerification(){EditorApplication.update+=PollPlayTest;EditorApplication.playModeStateChanged+=RestoreScenes;}
    public static void BeginPlayTest()
    {
        SessionState.SetString(TestKey+".original",string.Join("|",EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path)));
        var world=EditorSceneManager.OpenScene("Assets/01_Scenes/GameWorld.unity",OpenSceneMode.Additive);
        var root=EditorSceneManager.OpenScene("Assets/01_Scenes/GameRoot.unity",OpenSceneMode.Additive);
        SceneManager.SetActiveScene(root);
        var setup=root.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TestModeGameSceneSetup>(true)).First();
        var so=new SerializedObject(setup);so.FindProperty("_testSessionName").stringValue="CodexSupply-"+DateTime.UtcNow.ToString("HHmmss");var role=so.FindProperty("_playerPosition");role.enumValueIndex=Array.IndexOf(role.enumNames,"Builder");so.ApplyModifiedPropertiesWithoutUndo();
        SessionState.SetBool(TestKey,true);SessionState.SetFloat(TestKey+".start",(float)EditorApplication.timeSinceStartup);EditorApplication.isPlaying=true;
    }
    private static void RestoreScenes(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(TestKey+".restore",false))return;
        SessionState.SetBool(TestKey+".restore",false);
        string[] original=SessionState.GetString(TestKey+".original","").Split('|');
        for(int i=SceneManager.sceneCount-1;i>=0;i--){var s=SceneManager.GetSceneAt(i);if(!original.Contains(s.path))EditorSceneManager.CloseScene(s,true);}
    }
    private static void PollPlayTest()
    {
        if(!SessionState.GetBool(TestKey,false)||!EditorApplication.isPlaying)return;
        var report=new StringBuilder();
        try
        {
            var network=UnityEngine.Object.FindFirstObjectByType<RunnerSupplyNetwork>();
            if(network==null||!network.IsReady)
            {
                if(EditorApplication.timeSinceStartup-SessionState.GetFloat(TestKey+".start",0)<65)return;
                throw new Exception("Host test did not reach RunnerSupplyNetwork.IsReady within 65 seconds.");
            }
            SessionState.SetBool(TestKey,false);
            Require(network.HasStateAuthority,"Host owns supply state",report);
            var resource=UnityEngine.Object.FindFirstObjectByType<ResourceSystem>();resource.Mineral=1000;resource.Gas=1000;
            Require(network.RequestPurchase(7000)==RunnerSupplyResult.Pending,"Host purchase request dispatched",report);
            Require(network.Snapshot().SequenceEqual(new[]{7000})&&resource.Mineral==970,"Host purchase exact ID and debit",report);
            resource.Mineral=0;Require(network.RequestPurchase(7001)==RunnerSupplyResult.InsufficientResources&&network.Count==1,"Host insufficient purchase leaves queue intact",report);resource.Mineral=1000;
            var manager=UnityEngine.Object.FindFirstObjectByType<TowerBuildManager>();var builder=UnityEngine.Object.FindFirstObjectByType<PlayerBuilder>();
            var data=AssetDatabase.FindAssets("t:TowerData").Select(g=>AssetDatabase.LoadAssetAtPath<TowerData>(AssetDatabase.GUIDToAssetPath(g))).First(d=>d.Tower!=null&&d.Tower.TowerID==TowerIDContainer.SUPPLY_TOWER_ID);
            var grid=UnityEngine.Object.FindFirstObjectByType<InfiniteGrid>();Vector2Int spot=default;bool found=false;
            for(int x=-12;x<=12&&!found;x++)for(int y=-12;y<=12&&!found;y++)if(manager.CanBuildAt(data,builder,new Vector2Int(x,y))){spot=new Vector2Int(x,y);found=true;}
            Require(found,"Available supply tower placement",report);
            var method=typeof(TowerBuildManager).GetMethod("TryBuildTower",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            bool Build(int[] manifest,Vector2Int index)=>(bool)method.Invoke(manager,new object[]{data.TowerPrefabRef,data.Tower.TowerID,builder.Object.Id,index,manifest,builder.Object.InputAuthority});
            Require(!Build(new[]{7001},spot)&&network.Count==1,"Forged manifest rejected without consuming queue",report);
            Require(!Build(new[]{7000},new Vector2Int(9999,9999))&&network.Count==1,"Failed placement preserves purchased queue",report);
            Require(Build(new[]{7000},spot)&&network.Count==0,"Host construction consumes loaded item exactly once",report);
            var tower=UnityEngine.Object.FindFirstObjectByType<SupplyTower>();var runner=UnityEngine.Object.FindFirstObjectByType<PlayerRunner>();
            runner.transform.position=tower.transform.position;
            var inv=(RunnerItemInventory)typeof(PlayerRunner).GetField("_itemInventory",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(runner);
            tower.Interact(runner);Require(tower!=null&&tower.Object!=null&&tower.Object.IsValid,"Full inventory keeps item in tower",report);
            Require(inv.TryConsume(RunnerItemType.Lifeline),"Prepare one free item slot in test session",report);
            int[] before=Enumerable.Range(1,5).Select(i=>inv.GetSlot(i-1).Count).ToArray();tower.Interact(runner);
            Require(inv.GetSlot(0).Count==before[0]+1,"Tower delivers purchased Lifeline",report);
            for(int i=1;i<5;i++)Require(inv.GetSlot(i).Count==before[i],"Other item count unchanged "+i,report);
            Require(tower==null||tower.Object==null||!tower.Object.IsValid,"Empty supply tower despawned",report);
            report.AppendLine("Client peer and late join remain untested by this Host-only play session.");
            File.WriteAllText(Work+"/playtest.txt",report.ToString());
        }
        catch(Exception e){SessionState.SetBool(TestKey,false);File.WriteAllText(Work+"/playtest.txt",report+"FAIL: "+e);}
        SessionState.SetBool(TestKey+".restore",true);EditorApplication.isPlaying=false;
    }
    [MenuItem("ProjectIO/Runner Supply/Verify and Render Laboratory")]
    public static void Verify()
    {
        Directory.CreateDirectory(Work);
        var report=new StringBuilder();
        var catalog=AssetDatabase.LoadAssetAtPath<RunnerSupplyCatalog>(BuilderLaboratoryUIBuilder.CatalogPath);
        Require(catalog!=null&&catalog.Products.Count==7,"7 supply products",report);
        int[] mineral={30,30,30,50,0},gas={0,0,20,0,60};
        for(int i=0;i<5;i++)
        {
            Require(catalog.TryGet(7000+i,out var p)&&p.Icon!=null&&p.ItemType==(RunnerItemType)(i+1),"Item identity/icon "+(7000+i),report);
            Require(p.Cost.Mineral==mineral[i]&&p.Cost.Gas==gas[i],"Item sheet price "+p.Id,report);
            Require(RunnerSupplyRules.Evaluate(p,0,mineral[i],gas[i],~0)==RunnerSupplyResult.Success,"Exact balance accepted "+p.Id,report);
            Require(RunnerSupplyRules.Evaluate(p,10,999,999,~0)==RunnerSupplyResult.QueueFull,"Capacity rejection "+p.Id,report);
            Require(RunnerSupplyRules.Evaluate(p,0,0,0,~0)==RunnerSupplyResult.InsufficientResources,"Insufficient balance "+p.Id,report);
            if(p.RequiredCenter!=TowerPropertiesType.None)Require(RunnerSupplyRules.Evaluate(p,0,999,999,0)==RunnerSupplyResult.Locked,"Center unlock "+p.Id,report);
        }
        int[] queue={7000,7002,7000,7004};
        Require(RunnerSupplyRules.MatchesPrefix(queue,new[]{7000,7002}),"Snapshot prefix preserves order and duplicates",report);
        Require(!RunnerSupplyRules.MatchesPrefix(queue,new[]{7001})&&!RunnerSupplyRules.MatchesPrefix(queue,Array.Empty<int>()),"Forged/empty tower manifest rejected",report);
        var consumer=new RunnerItemConsumer(Array.Empty<IItemConsumptionStrategy>());
        var definitions=Enumerable.Range(1,5).Select(i=>new RunnerItemDefinition((RunnerItemType)i,0,1)).ToArray();
        var inventory=new RunnerItemInventory(consumer,definitions);
        Require(inventory.TryAdd((int)RunnerSupplyRules.GetItemType(7003)-1,1),"Purchased grenade accepted",report);
        for(int i=0;i<5;i++)Require(inventory.GetSlot(i).Count==(i==3?1:0),"Only selected inventory slot changes "+i,report);
        Require(!inventory.TryAdd(3,1),"Full slot rejects delivery without spillover",report);
        var lab=AssetDatabase.LoadAssetAtPath<GameObject>(BuilderLaboratoryUIBuilder.PrefabPath);
        Require(lab.GetComponentsInChildren<Button>(true).Length==19,"13 upgrades, 2 tabs, 3 purchases and close",report);
        Require(lab.GetComponentsInChildren<LaboratorySupplySlotUI>(true).Length==10,"10 item image slots",report);
        var dd=lab.GetComponentInChildren<TMP_Dropdown>(true);
        Require(dd!=null&&dd.options.Count==5&&dd.template!=null&&dd.itemImage!=null,"Dropdown template and 5 item options",report);
        Require(dd.targetGraphic.raycastTarget&&dd.template.GetComponentInChildren<Toggle>(true).targetGraphic.raycastTarget,"Dropdown and options accept pointer input",report);
        var builderPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Prefabs/UI/Builder/Player Builder UI.prefab");
        var close=builderPrefab.GetComponentsInChildren<Button>(true).First(b=>b.name=="Close Button");
        Require(close.onClick.GetPersistentEventCount()==1&&close.onClick.GetPersistentTarget(0)!=null&&close.onClick.GetPersistentMethodName(0)=="OnClickLaboratoryButton","Nested prefab close event preserved",report);
        foreach(var c in lab.GetComponentsInChildren<MonoBehaviour>(true))Require(c!=null,"UI script reference",report);
        foreach(var c in lab.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c is LaboratoryItemDropdownUI||c is LaboratorySupplySlotUI||c is LaboratorySupplyInventoryUI||c is RunnerSupplyUI||c is LaboratoryTabsUI||c is LaboratoryUI||c is TowerUpgradeUI||c is RunnerUpgradeUI))
        {
            var iterator=new SerializedObject(c).GetIterator();
            while(iterator.NextVisible(true))if(iterator.propertyType==SerializedPropertyType.ObjectReference)Require(iterator.objectReferenceValue!=null,c.GetType().Name+"."+iterator.propertyPath,report);
        }
        var networkPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Prefabs/Laboratory/Laboratory.prefab");
        Require(networkPrefab.GetComponent<RunnerSupplyNetwork>().Catalog==catalog,"Laboratory catalog injected",report);
        var interest=new SerializedObject(networkPrefab.GetComponent<Fusion.NetworkObject>()).FindProperty("ObjectInterest");
        report.AppendLine("Network interest: "+interest.enumDisplayNames[interest.enumValueIndex]);
        Render(1920,1080,false,false,catalog);Render(1280,720,false,false,catalog);Render(1920,1080,true,true,catalog);
        report.AppendLine("Runtime Host/Client, unauthorized RPC, late join, tower spawn failure, partial receive and despawn require play sessions; not covered by these editor checks.");
        File.WriteAllText(Work+"/verification.txt",report.ToString());
    }
    private static void Require(bool value,string message,StringBuilder report){if(!value)throw new Exception("FAIL: "+message);report.AppendLine("PASS: "+message);}
    private static void Render(int width,int height,bool runner,bool dropdown,RunnerSupplyCatalog catalog)
    {
        var scene=EditorSceneManager.NewPreviewScene();
        var rt=new RenderTexture(width,height,24);rt.Create();
        try
        {
            var cameraObject=new GameObject("Preview Camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.GetComponent<Camera>();camera.scene=scene;camera.targetTexture=rt;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.01f,.02f,.028f);camera.orthographic=true;camera.orthographicSize=height/2f;camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.transform.position=new Vector3(0,0,-10);
            var canvasObject=new GameObject("Preview Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));SceneManager.MoveGameObjectToScene(canvasObject,scene);
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BuilderLaboratoryUIBuilder.PrefabPath),scene);root.transform.SetParent(canvasObject.transform,false);root.SetActive(true);
            if(runner)root.GetComponentInChildren<LaboratoryTabsUI>().ShowRunner();
            root.GetComponentInChildren<LaboratoryItemDropdownUI>().Populate();
            root.GetComponentInChildren<LaboratorySupplyInventoryUI>().DisplaySnapshot(catalog,new[]{7000,7001,7002,7003,7004,7000,1,2,7001,7003});
            foreach(var text in root.GetComponentsInChildren<TMP_Text>(true)){if(text.name=="Minerals")text.text="광물  240";if(text.name=="Gas")text.text="가스  180";text.ForceMeshUpdate(true);}
            Canvas.ForceUpdateCanvases();
            if(dropdown){var d=root.GetComponentInChildren<TMP_Dropdown>();d.SendMessage("Start");d.Show();var list=d.transform.Find("Dropdown List");if(list!=null)list.GetComponent<CanvasGroup>().alpha=1;Canvas.ForceUpdateCanvases();}
            camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(width,height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();RenderTexture.active=old;
            File.WriteAllBytes(Work+"/laboratory-"+width+(dropdown?"-dropdown":"")+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
        }
        finally{rt.Release();UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
