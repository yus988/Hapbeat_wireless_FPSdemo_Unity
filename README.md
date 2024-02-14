# Hapbeat_wireless_FPSdemo_Unity
Unity version 2021.3.34f1

1. SerialHandler GameObject を作成し、SerialHandler.cs をアタッチ


シリアル送信関数
SendSerial(string message, int position, float power[2])
- message：イベントの種類
 - gunshot
 - walk
- position：装着部位（首以外は予定）
 - neck: 首（ネックレス）
 - wrist_L: 腕・左（リストバンド）
 - wrist_R: 腕・右（リストバンド）
- power：振動強度（0--1 float）


### 銃撃時に振動を付ける場合
`scripts/Gameplay/Managers/PlayerWeaponsManager.cs` 
- Start() -> SerialHandler をアタッチ

 - private SerialHandler _serialhandler;
_serialhandler = GameObject.Find("SerialHandler").GetComponent<SerialHandler>();

- Update() -> if(hasFired) 内に銃撃時の処理
 - _serialhandler.SendSerial("gunshot", 0.5f);

### 歩行時に振動を付ける場合
PlayerCharacterController.cs->HandleCharacterMovement->if (m_FootstepDistanceCounter >= 1f / chosenFootstepSfxFrequency)

### ジャンプ着地時
PlayerCharacterController.cs->Update()->if (IsGrounded && !wasGrounded):

### プレイヤー被弾時
ProjectileStandard.cs->OnHit:

