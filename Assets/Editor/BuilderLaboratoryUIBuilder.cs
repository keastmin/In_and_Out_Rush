using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KIM.Dev;
using ProjectIO.RunnerSupply;

[InitializeOnLoad]
public static class BuilderLaboratoryUIBuilder
{
    public const string PrefabPath = "Assets/03_Prefabs/UI/Builder/Laboratory/Builder Laboratory UI.prefab";
    public const string CatalogPath = "Assets/08_Data/Runner Supply Catalog.asset";
    private const string Work = "Library/RunnerSupplyTools";
    static BuilderLaboratoryUIBuilder() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Work + "/request.txt")) return;
        string command = File.ReadAllText(Work + "/request.txt").Trim();
        File.Delete(Work + "/request.txt");
        if(File.Exists(Work+"/error.txt"))File.Delete(Work+"/error.txt");
        try { if (command == "inspect") Inspect(); else if (command == "build") Build(); else if(command=="verify")RunnerSupplyVerification.Verify(); else if(command=="playtest")RunnerSupplyVerification.BeginPlayTest(); }
        catch (Exception e) { File.WriteAllText(Work + "/error.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("ProjectIO/Runner Supply/Inspect Laboratory")]
    public static void Inspect()
    {
        Directory.CreateDirectory(Work);
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var b = new StringBuilder();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            b.Append(new string(' ', Depth(t, root.transform) * 2)).Append(t.name).Append(" :: ");
            b.AppendLine(string.Join(",", t.GetComponents<Component>().Select(c => c ? c.GetType().Name : "MISSING")));
            foreach (var button in t.GetComponents<Button>())
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    b.AppendLine("  EVENT " + button.onClick.GetPersistentTarget(i) + " -> " + button.onClick.GetPersistentMethodName(i));
        }
        foreach(var tmp in root.GetComponentsInChildren<TMP_Text>(true).Take(3)) b.AppendLine("FONT " + AssetDatabase.GetAssetPath(tmp.font));
        File.WriteAllText(Work + "/hierarchy.txt", b.ToString());
    }
    private static int Depth(Transform t, Transform root) { int d=0; while(t!=root && t.parent!=null){d++;t=t.parent;} return d; }

    private static TMP_FontAsset _font;
    private static readonly Color Ink = new Color(.025f,.045f,.065f,1);
    private static readonly Color Card = new Color(.06f,.105f,.145f,1);
    private static readonly Color Accent = new Color(.12f,.7f,.77f,1);
    private const string Art = "Assets/06_Sprites/UI/BuilderLaboratory/Generated/";

    [MenuItem("ProjectIO/Runner Supply/Build Laboratory")]
    public static void Build()
    {
        Directory.CreateDirectory(Work);
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/11_Fonts/CasquareCode35-Bold SDF.asset");
        Sprite headerSprite = ImportSprite(Art+"laboratory-header-v2.png", false);
        Sprite panelSprite = ImportSprite(Art+"laboratory-panel-v2.png", true);
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var tower = root.GetComponentInChildren<TowerUpgradeUI>(true);
            var runner = root.GetComponentInChildren<RunnerUpgradeUI>(true);
            var supply = root.GetComponentInChildren<RunnerSupplyUI>(true);
            var inventory = root.GetComponentInChildren<LaboratorySupplyInventoryUI>(true);
            var close = root.GetComponentsInChildren<Button>(true).First(b=>b.name=="Close Button");
            var previous=AssetDatabase.LoadAssetAtPath<RunnerSupplyCatalog>(CatalogPath);
            var skillIcon = previous!=null?previous.Products[0].Icon:supply.transform.Find("Supply Purchase Buttons/Skill Supply Button/Icon Frame/Icon Image").GetComponent<Image>().sprite;
            var weaponIcon = previous!=null?previous.Products[1].Icon:supply.transform.Find("Supply Purchase Buttons/Weapon Supply Button/Icon Frame/Icon Image").GetComponent<Image>().sprite;
            RunnerSupplyCatalog catalog = CreateCatalog(skillIcon, weaponIcon);
            // Preserve the original components, upgrade button IDs, and external close-button reference.
            foreach (var c in new Component[]{tower,runner,supply,inventory,close}) c.transform.SetParent(root.transform,false);
            foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                if (child!=tower.transform && child!=runner.transform && child!=supply.transform && child!=inventory.transform && child!=close.transform)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            RectTransform rr = (RectTransform)root.transform;
            Place(rr, .055f,.06f,.945f,.94f,0,0,0,0);
            rr.localScale=Vector3.one;
            var back=Panel(root.transform,"00 Background",0,0,1,1,Ink); back.raycastTarget=true;back.transform.SetAsFirstSibling();
            var header=Panel(root.transform,"01 Header",0,.85f,1,1,Color.white,headerSprite);
            var title=Label(header.transform,"Title","연구소",36); Place(title.rectTransform,0,.40f,.45f,1,32,0,0,-10);
            var sub=Label(header.transform,"Subtitle","LABORATORY  /  연구 · 강화 · 보급",17); Place(sub.rectTransform,0,0,.52f,.42f,34,12,0,0); sub.color=new Color(.55f,.74f,.8f);
            var minerals=Label(header.transform,"Minerals","광물  —",24); Place(minerals.rectTransform,.54f,.3f,.73f,.75f,0,0,0,0);
            var gas=Label(header.transform,"Gas","가스  —",24); Place(gas.rectTransform,.74f,.3f,.9f,.75f,0,0,0,0);
            close.transform.SetParent(header.transform,false); Place((RectTransform)close.transform,1,1,1,1,-60,-58,-18,-16);
            close.image.sprite=null; close.image.color=Card;
            foreach(var t in close.GetComponentsInChildren<TMP_Text>()){Style(t,26);t.text="×";}

            var upgrades=Panel(root.transform,"02 Research",.018f,.235f,.60f,.828f,Color.white,panelSprite);
            var tabs=upgrades.gameObject.AddComponent<LaboratoryTabsUI>();
            var towerTab=MakeButton(upgrades.transform,"Tower Tab","타워 강화",null);
            Place((RectTransform)towerTab.transform,0,.885f,.5f,1,20,0,-6,-18);
            var runnerTab=MakeButton(upgrades.transform,"Runner Tab","러너 강화",null);
            Place((RectTransform)runnerTab.transform,.5f,.885f,1,1,6,0,-20,-18);
            UnityEventTools.AddPersistentListener(towerTab.onClick,tabs.ShowTower);
            UnityEventTools.AddPersistentListener(runnerTab.onClick,tabs.ShowRunner);
            ConfigureUpgradePage(tower,upgrades.transform);
            ConfigureUpgradePage(runner,upgrades.transform);
            Set(tabs,"_towerPage",tower.gameObject);Set(tabs,"_runnerPage",runner.gameObject);
            Set(tabs,"_towerTab",towerTab.image);Set(tabs,"_runnerTab",runnerTab.image);tabs.ShowTower();

            supply.transform.SetParent(root.transform,false); ClearChildren(supply.transform); StripLayouts(supply.gameObject);
            Place((RectTransform)supply.transform,.617f,.235f,.982f,.828f,0,0,0,0);
            var si=supply.GetComponent<Image>();si.sprite=panelSprite;si.type=Image.Type.Sliced;si.color=Color.white;
            var st=Label(supply.transform,"Title","RUNNER SUPPLY",26);Place(st.rectTransform,0,.88f,1,1,26,0,-26,-14);
            var skill=MakeButton(supply.transform,"Skill Supply","스킬 보급",supply.OnClickSkillSupplyButton);
            Place((RectTransform)skill.transform,0,.70f,.5f,.86f,24,0,-7,0);
            var weapon=MakeButton(supply.transform,"Weapon Supply","무기 보급",supply.OnClickWeaponSupplyButton);
            Place((RectTransform)weapon.transform,.5f,.70f,1,.86f,7,0,-24,0);
            var skPrice=SupplyPrice(skill,skillIcon);var wePrice=SupplyPrice(weapon,weaponIcon);
            var ititle=Label(supply.transform,"Item Label","아이템 선택",19);Place(ititle.rectTransform,0,.615f,1,.70f,26,0,-26,0);
            var picker=Rect(supply.transform,"Item Purchase"); Place(picker,0,.10f,1,.615f,24,0,-24,0);
            var itemUI=picker.gameObject.AddComponent<LaboratoryItemDropdownUI>();
            TMP_Dropdown dropdown=MakeDropdown(picker);Place((RectTransform)dropdown.transform,0,.78f,1,1,0,0,0,0);
            var selected=Panel(picker,"Selected Icon",0,.30f,.20f,.69f,Color.white);selected.preserveAspect=true;
            var description=Label(picker,"Description","",19);Place(description.rectTransform,.23f,.39f,1,.70f,8,0,0,0);description.textWrappingMode=TextWrappingModes.Normal;
            var price=Label(picker,"Price","",22);Place(price.rectTransform,.23f,.23f,1,.40f,8,0,0,0);price.color=Accent;
            var availability=Label(picker,"Availability","",16);Place(availability.rectTransform,0,.0f,.64f,.22f,0,0,-8,0);availability.textWrappingMode=TextWrappingModes.Normal;
            var buy=MakeButton(picker,"Purchase Item","구매",supply.OnClickItemSupplyButton);Place((RectTransform)buy.transform,.67f,0,1,.21f,0,0,0,0);buy.image.color=new Color(.08f,.36f,.43f);
            Set(itemUI,"_catalog",catalog);Set(itemUI,"_dropdown",dropdown);Set(itemUI,"_selectedIcon",selected);
            Set(itemUI,"_description",description);Set(itemUI,"_price",price);Set(itemUI,"_availability",availability);Set(itemUI,"_purchaseButton",buy);
            UnityEventTools.AddPersistentListener(dropdown.onValueChanged,itemUI.OnSelectionChanged);
            itemUI.Populate();
            var feedback=Label(supply.transform,"Purchase Feedback","보급 타워를 건설하면 대기 중인 보급품이 적재됩니다.",15);Place(feedback.rectTransform,0,0,1,.10f,26,10,-26,0);feedback.textWrappingMode=TextWrappingModes.Normal;
            Set(supply,"_items",itemUI);Set(supply,"_skillButton",skill);Set(supply,"_weaponButton",weapon);
            Set(supply,"_skillPrice",skPrice);Set(supply,"_weaponPrice",wePrice);Set(supply,"_mineral",minerals);Set(supply,"_gas",gas);Set(supply,"_feedback",feedback);

            inventory.transform.SetParent(root.transform,false);ClearChildren(inventory.transform);StripLayouts(inventory.gameObject);
            Place((RectTransform)inventory.transform,.018f,.022f,.982f,.215f,0,0,0,0);
            var invImage=inventory.GetComponent<Image>();invImage.sprite=null;invImage.color=Card;
            var qtitle=Label(inventory.transform,"Title","보급 대기열",23);Place(qtitle.rectTransform,0,.69f,.25f,1,20,0,0,0);
            var qhelp=Label(inventory.transform,"Hint","구매한 순서대로 적재 · 러너가 보급 타워에서 F로 수령",16);Place(qhelp.rectTransform,.24f,.69f,.86f,1,0,0,0,0);
            var count=Label(inventory.transform,"Count","0 / 10",22);Place(count.rectTransform,.86f,.69f,1,1,0,0,-20,0);count.alignment=TextAlignmentOptions.MidlineRight;
            var grid=Rect(inventory.transform,"Slots");Place(grid,0,0,1,.69f,18,12,-18,0);
            var slots=new LaboratorySupplySlotUI[10];
            for(int i=0;i<10;i++)
            {
                var cell=Panel(grid,"Slot "+(i+1).ToString("00"),i/10f,0,(i+1)/10f,1,Ink);cell.rectTransform.offsetMin=new Vector2(4,0);cell.rectTransform.offsetMax=new Vector2(-4,0);
                var icon=Panel(cell.transform,"Item Icon",.28f,.30f,.72f,.91f,Color.white);icon.preserveAspect=true;
                var name=Label(cell.transform,"Item Name","",18);Place(name.rectTransform,0,0,1,.28f,3,0,-3,0);name.alignment=TextAlignmentOptions.Center;
                var order=Label(cell.transform,"Order","",13);Place(order.rectTransform,0,.73f,.25f,1,7,0,0,0);order.color=new Color(.4f,.56f,.63f);
                var empty=Label(cell.transform,"Empty","—",24);Place(empty.rectTransform,0,.20f,1,.9f,0,0,0,0);empty.alignment=TextAlignmentOptions.Center;empty.color=new Color(.2f,.32f,.4f);
                var slot=cell.gameObject.AddComponent<LaboratorySupplySlotUI>();slots[i]=slot;
                Set(slot,"_icon",icon);Set(slot,"_name",name);Set(slot,"_order",order);Set(slot,"_empty",empty.gameObject);slot.Display(i,null);
            }
            var iso=new SerializedObject(inventory);var sp=iso.FindProperty("_slots");sp.arraySize=10;for(int i=0;i<10;i++)sp.GetArrayElementAtIndex(i).objectReferenceValue=slots[i];iso.ApplyModifiedPropertiesWithoutUndo();Set(inventory,"_count",count);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            ConfigureNetwork(catalog);
            CleanPresentationOverrides();
            File.WriteAllText(Work+"/build.txt","Laboratory prefab, catalog and network binding saved.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static RunnerSupplyCatalog CreateCatalog(Sprite skill, Sprite weapon)
    {
        var runnerUI=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Prefabs/UI/Runner/Player Runner UI.prefab");
        var playerUI=runnerUI.GetComponent<PlayerRunnerUI>();
        var slots=new SerializedObject(playerUI).FindProperty("itemSlotViews");
        var icons=new Sprite[5];
        for(int i=0;i<5;i++)
        {
            var slot=slots.GetArrayElementAtIndex(i).objectReferenceValue;
            var icon=(Image)new SerializedObject(slot).FindProperty("iconImage").objectReferenceValue;
            icons[i]=icon.sprite;
        }
        var catalog=AssetDatabase.LoadAssetAtPath<RunnerSupplyCatalog>(CatalogPath);
        if(catalog==null){catalog=ScriptableObject.CreateInstance<RunnerSupplyCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
        string[] names={"스킬 보급","무기 보급","생명선","방벽","소각기","전류탄","생분해 장치"};
        string[] descriptions={"러너 스킬 보급","러너 무기 보급","경로 복귀를 돕는 생존 아이템","피해를 막는 방어 아이템","화염으로 적을 공격하는 아이템","전류로 적을 제압하는 투척 아이템","생분해 효과로 적을 공격하는 아이템"};
        int[] ids={1,2,7000,7001,7002,7003,7004};int[] mineral={50,50,30,30,30,50,0};int[] gas={50,50,0,0,20,0,60};
        int[] centers={0,0,0,0,(int)TowerPropertiesType.Flame,(int)TowerPropertiesType.Blitz,(int)TowerPropertiesType.Biochemical};
        var so=new SerializedObject(catalog);var products=so.FindProperty("_products");products.arraySize=7;
        for(int i=0;i<7;i++)
        {
            var p=products.GetArrayElementAtIndex(i);p.FindPropertyRelative("Id").intValue=ids[i];p.FindPropertyRelative("Name").stringValue=names[i];
            p.FindPropertyRelative("Icon").objectReferenceValue=i==0?skill:i==1?weapon:icons[i-2];
            p.FindPropertyRelative("ItemType").intValue=i<2?0:i-1;p.FindPropertyRelative("RequiredCenter").intValue=centers[i];p.FindPropertyRelative("Description").stringValue=descriptions[i];
            p.FindPropertyRelative("Cost").FindPropertyRelative("Mineral").intValue=mineral[i];p.FindPropertyRelative("Cost").FindPropertyRelative("Gas").intValue=gas[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssetIfDirty(catalog);return catalog;
    }
    private static void ConfigureNetwork(RunnerSupplyCatalog catalog)
    {
        const string path="Assets/03_Prefabs/Laboratory/Laboratory.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{var network=root.GetComponent<RunnerSupplyNetwork>()??root.AddComponent<RunnerSupplyNetwork>();Set(network,"_catalog",catalog);PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    private static void CleanPresentationOverrides()
    {
        const string builderPath="Assets/03_Prefabs/UI/Builder/Player Builder UI.prefab";
        var builder=PrefabUtility.LoadPrefabContents(builderPath);
        try
        {
            var lab=builder.GetComponentInChildren<LaboratoryUI>(true);
            FilterOverrides(lab.gameObject);
            PrefabUtility.SaveAsPrefabAsset(builder,builderPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(builder);}
        const string scenePath="Assets/01_Scenes/GamePresentation.unity";
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
        bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
        bool changed=false;
        foreach(var root in scene.GetRootGameObjects())
            foreach(var b in root.GetComponentsInChildren<PlayerBuilderUI>(true))
                changed|=FilterOverrides(PrefabUtility.GetOutermostPrefabInstanceRoot(b.gameObject));
        if(changed)UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
    }
    private static bool FilterOverrides(GameObject instance)
    {
        if(instance==null)return false;
        var mods=PrefabUtility.GetPropertyModifications(instance);if(mods==null)return false;
        var filtered=mods.Where(m=>m.target!=null && (!IsLaboratoryTarget(m.target) ||
            m.propertyPath.StartsWith("m_OnClick") || (m.target is GameObject g && g.name=="Builder Laboratory UI" && m.propertyPath=="m_IsActive"))).ToArray();
        if(filtered.Length==mods.Length)return false;
        PrefabUtility.SetPropertyModifications(instance,filtered);return true;
    }
    private static bool IsLaboratoryTarget(UnityEngine.Object target)
    {
        if(AssetDatabase.GetAssetPath(target)==PrefabPath)return true;
        var go=target as GameObject;if(target is Component c)go=c.gameObject;
        return go!=null&&go.GetComponentInParent<LaboratoryUI>(true)!=null;
    }
    private static void ConfigureUpgradePage(Component controller,Transform parent)
    {
        var buttons=controller.GetComponentsInChildren<Button>(true);
        controller.transform.SetParent(parent,false);StripLayouts(controller.gameObject);
        Place((RectTransform)controller.transform,0,0,1,.86f,20,20,-20,0);
        controller.GetComponent<Image>().sprite=null;controller.GetComponent<Image>().color=Color.clear;
        var content=Rect(controller.transform,"Upgrade Rows");
        foreach(var button in buttons) button.transform.SetParent(content,false);
        foreach(Transform t in controller.transform.Cast<Transform>().ToArray())if(t!=content)UnityEngine.Object.DestroyImmediate(t.gameObject);
        var viewport=Rect(controller.transform,"Viewport");Place(viewport,0,0,1,1,0,0,0,0);viewport.gameObject.AddComponent<RectMask2D>();content.SetParent(viewport,false);
        Place(content,0,1,1,1,0,-buttons.Length*92,0,0);content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;
        var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=10;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;
        var fitter=content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=controller.gameObject.GetComponent<ScrollRect>()??controller.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.scrollSensitivity=35;scroll.movementType=ScrollRect.MovementType.Clamped;
        viewport.offsetMax=new Vector2(-16,0);
        var bar=Panel(controller.transform,"Scrollbar",1,0,1,1,Ink);Place(bar.rectTransform,1,0,1,1,-9,0,0,0);
        var handle=Panel(bar.transform,"Handle",0,0,1,1,Accent);handle.raycastTarget=true;
        var scrollbar=bar.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle.rectTransform;scrollbar.targetGraphic=handle;scrollbar.direction=Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar=scrollbar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        scrollbar.SetValueWithoutNotify(1);scroll.verticalNormalizedPosition=1;
        foreach(var b in buttons)
        {
            b.image.sprite=null;b.image.color=Card;var le=b.GetComponent<LayoutElement>();le.minHeight=82;le.preferredHeight=82;le.flexibleHeight=0;
            foreach(var text in b.GetComponentsInChildren<TMP_Text>())Style(text,text.name.Contains("Name")?23:20);
            var name=(RectTransform)b.transform.Find("Upgrade Name Text");Place(name,0,.48f,.76f,1,86,0,0,-5);
            var level=(RectTransform)b.transform.Find("Level Text");Place(level,.78f,0,1,1,0,0,-20,0);level.GetComponent<TMP_Text>().alignment=TextAlignmentOptions.MidlineRight;
            var cost=(RectTransform)b.transform.Find("Cost Row");Place(cost,0,0,.77f,.48f,86,6,0,0);
            var frame=(RectTransform)b.transform.Find("Icon Frame");Place(frame,0,.5f,0,.5f,12,-28,68,28);
        }
    }
    private static TMP_Text SupplyPrice(Button button,Sprite sprite)
    {
        var title=button.GetComponentInChildren<TMP_Text>();Place(title.rectTransform,0,.43f,1,1,49,0,-8,0);title.fontSize=20;title.alignment=TextAlignmentOptions.MidlineLeft;
        var icon=Panel(button.transform,"Icon",0,.50f,0,.50f,Color.white);Place(icon.rectTransform,0,.5f,0,.5f,10,-19,44,19);icon.sprite=sprite;icon.preserveAspect=true;
        var price=Label(button.transform,"Price","광물 50 · 가스 50",18);Place(price.rectTransform,0,0,1,.44f,49,3,-6,0);return price;
    }
    private static TMP_Dropdown MakeDropdown(Transform parent)
    {
        var bg=Panel(parent,"Item Dropdown",0,0,1,1,Card);bg.raycastTarget=true;var d=bg.gameObject.AddComponent<TMP_Dropdown>();d.targetGraphic=bg;
        var caption=Label(bg.transform,"Selected Item","",19);Place(caption.rectTransform,0,0,1,1,48,0,-30,0);
        var capIcon=Panel(bg.transform,"Selected Item Icon",0,.5f,0,.5f,Color.white);Place(capIcon.rectTransform,0,.5f,0,.5f,8,-16,40,16);capIcon.preserveAspect=true;
        var arrow=Label(bg.transform,"Arrow","▾",22);Place(arrow.rectTransform,1,0,1,1,-30,0,-6,0);
        var template=Panel(bg.transform,"Template",0,0,1,0,Ink);Place(template.rectTransform,0,0,1,0,0,-288,0,-8);template.rectTransform.pivot=new Vector2(.5f,1);template.rectTransform.anchoredPosition=new Vector2(0,-8);
        var viewport=Rect(template.transform,"Viewport");Place(viewport,0,0,1,1,4,4,-4,-4);viewport.gameObject.AddComponent<RectMask2D>();
        var content=Rect(viewport,"Content");Place(content,0,1,1,1,0,-52,0,0);content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;
        var row=Panel(content,"Item",0,.5f,1,.5f,Card);Place(row.rectTransform,0,.5f,1,.5f,0,-26,0,26);
        row.raycastTarget=true;var toggle=row.gameObject.AddComponent<Toggle>();toggle.targetGraphic=row;
        var check=Panel(row.transform,"Selection",0,0,0,1,Accent);Place(check.rectTransform,0,0,0,1,0,4,3,-4);toggle.graphic=check;toggle.isOn=true;
        var icon=Panel(row.transform,"Item Icon",0,.5f,0,.5f,Color.white);Place(icon.rectTransform,0,.5f,0,.5f,10,-18,46,18);icon.preserveAspect=true;
        var label=Label(row.transform,"Item Label","",19);Place(label.rectTransform,0,0,1,1,55,0,-8,0);
        var scroll=template.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.scrollSensitivity=30;scroll.movementType=ScrollRect.MovementType.Clamped;
        d.template=template.rectTransform;d.captionText=caption;d.captionImage=capIcon;d.itemText=label;d.itemImage=icon;template.gameObject.SetActive(false);return d;
    }
    private static Sprite ImportSprite(string path,bool sliced)
    {
        AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.spriteBorder=sliced?new Vector4(64,64,64,64):Vector4.zero;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static void Set(UnityEngine.Object target,string field,UnityEngine.Object value){var so=new SerializedObject(target);var p=so.FindProperty(field);if(p==null)throw new Exception(target.name+" missing "+field);p.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    private static void StripLayouts(GameObject go){foreach(var c in go.GetComponents<Component>())if(c is LayoutGroup||c is LayoutElement||c is ContentSizeFitter||c.GetType().Name=="LaboratoryWidthLayoutConstraint")UnityEngine.Object.DestroyImmediate(c);}
    private static void ClearChildren(Transform t){foreach(Transform c in t.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(c.gameObject);}
    private static RectTransform Rect(Transform parent,string name){var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);return (RectTransform)go.transform;}
    private static Image Panel(Transform parent,string name,float x0,float y0,float x1,float y1,Color color,Sprite sprite=null){var r=Rect(parent,name);Place(r,x0,y0,x1,y1,0,0,0,0);var i=r.gameObject.AddComponent<Image>();i.color=color;i.sprite=sprite;i.type=sprite!=null&&sprite.border!=Vector4.zero?Image.Type.Sliced:Image.Type.Simple;i.raycastTarget=false;return i;}
    private static TextMeshProUGUI Label(Transform parent,string name,string text,float size){var r=Rect(parent,name);var t=r.gameObject.AddComponent<TextMeshProUGUI>();Style(t,size);t.text=text;return t;}
    private static void Style(TMP_Text t,float size){t.font=_font;t.fontSize=size;t.enableAutoSizing=false;t.color=new Color(.88f,.94f,.97f);t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;}
    private static Button MakeButton(Transform p,string name,string text,UnityAction action){var i=Panel(p,name,0,0,1,1,Card);i.raycastTarget=true;var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;var t=Label(i.transform,"Label",text,23);Place(t.rectTransform,0,0,1,1,8,0,-8,0);t.alignment=TextAlignmentOptions.Center;if(action!=null)UnityEventTools.AddPersistentListener(b.onClick,action);return b;}
    private static void Place(RectTransform r,float x0,float y0,float x1,float y1,float left,float bottom,float right,float top){r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.pivot=new Vector2(.5f,.5f);r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(right,top);r.localScale=Vector3.one;}
}
