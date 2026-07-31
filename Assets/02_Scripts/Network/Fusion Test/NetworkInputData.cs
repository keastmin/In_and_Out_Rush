using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public const byte MOUSEBUTTON0 = 1; // 빌더의 타워 설치
    public const byte MOUSEBUTTON1 = 2; // 우클릭
    public const byte DASH_INPUT = 3; // 러너의 대쉬
    public const byte SLIDE_INPUT = 4; // 러너의 슬라이드
    public const byte ITEM_INPUT = 5; // 러너의 아이템 사용
    public const byte SKILL_INPUT = 6; // 러너의 스킬 사용
    public const byte INTERACT_INPUT = 7; // 러너의 상호작용
    public const byte WEAPON_INPUT = 9; // 러너의 무기 사용


    // 러너 입력
    public NetworkButtons DashInput;
    public NetworkButtons SlideInput;
    public Vector3 PlayerRunnerDirection;
    public NetworkButtons ItemInput;
    public int SelectedItem;
    public NetworkButtons SkillInput;
    public int SelectedSkill;
    public NetworkButtons InteractInput;
    public NetworkButtons WeaponInput;

    // 빌더 입력
    public NetworkButtons MouseButton0; // 빌더의 타워 설치에 필요한 좌클릭 
    public NetworkButtons MouseButton1; // 빌더의 타워 취소에 필요한 우클릭
    public Vector3 MousePosition; // 타워를 설치할 위치에 해당하는 마우스 위치
}
