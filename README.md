# Hapbeat_wireless_FPSdemo_Unity

1. SerialHandler GameObject を作成し、SerialHandler.cs をアタッチ
2. 

### 銃撃時に音を付ける場合
`scripts/Gameplay/Managers/PlayerWeaponsManager.cs` 
Start() -> SerialHandler をアタッチ
Update() -> if(hasFired) 内に銃撃時の処理

