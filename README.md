![Untitled](https://github.com/jellypower/PublicImageDataBase/blob/main/Portfolio/UnityRasberryPIVRController/GamePlayVideoBanner.png?raw=true)

> 2022년도 1학기, 세종대학교 임베디드 프로젝트 수업 과정에서 제작한
**“라즈베리파이를 활용한 VR컨트롤러 제작”** 프로젝트
> 

# 프로젝트 소개


- 주제: 라즈베리파이와 유니티를 이용한 건슈팅 컨트롤러 제작
- 프로젝트 내용:
    - 라즈베리파이에 부착한 센서의 값을 [RTIMULib](https://github.com/HongshiTan/RTIMULib2/tree/master/RTIMULib)라는 오픈소스 라이브러리를 통해 샘플링
    - 샘플링값을 컨트롤러로 활용할 수 있게 패킷화하여 타겟 디바이스에 UDP전송
    - 타겟 디바이스에서 활용 가능한 Unity, C#기반 패킷 수신 API 제작
    - UDP 브로드캐스팅기반 기기 탐색 및 인증 자동화
    - API와 데모게임과 연동 및 시연
    

## 프로젝트 스펙

1. Sensor: Sense HAT
• STM/LSM9DS1(9dof-IMU), Joystick
2. Platform:
• 컨트롤러: Rasberry Pi 4
• 클라이언트: Unity, x86, Windows
3. References:
    - RTIMULib (https://github.com/RPi-Distro/RTIMULib , 컨트롤러)
        - 라즈베리파이4에 장착 가능한 Sense HAT센서로부터 값을 읽어오는 API
    - FPS Microgame(https://learn.unity.com/project/fps-maikeurogeim , 게임)
        - 유니티에서 제공하는 오픈소스 FPS게임 샘플

# 기술명세


- UDP Broadcast를 활용해 LAN내에서 활용 가능한 기기 탐색 및 인증을 자동화

![Untitled](https://github.com/jellypower/PublicImageDataBase/blob/main/Portfolio/UnityRasberryPIVRController/ValidationProcess.png)


- Unity, C#라이브러리가 패킷을 수신 하는 과정에서 소켓의 블로킹으로 인해 프레임이 멈추는 현상을 방지하기 위해 별도의 스레드를 생성하여 패킷 수신 작업을 비동기로 진행

```cpp

public class MyVRControllerManager : MonoBehaviour
{
	.
	.
	.
    UdpClient cli;
	.
	.
	.
	void StartRecvControllerInfo()
  {
		Thread thread = new Thread(ReadSensor);
		thread.Start();
	}

	void ReadSensor()
  {
		while (state == NetworkState.Connected)
		{
			try
			{
				bytes = cli.Receive(ref endPoint);
			}
			catch (SocketException e)
			{
				print(e);
				state = NetworkState.Disconnected;
				return;
			}
			UpdateDevData(bytes);
			.
			.
			.
	}
	.
	.
	.
```

- 소스코드: https://github.com/jellypower/RasPiGunshootGameDemo/blob/master/Assets/FPS/Scripts/Game/VRControllerScript/MyVRControllerManager.cs


- 센서 데이터를 Quaternion형태로 가공하고 샘플링, 전송하여 짐벌락 현상으로 인한 컨트롤러 회전값 계산과정에서의 노이즈 제거

```cpp
void updateVRData(struct packetData* data, RTIMU_DATA* sensor, struct pollfd* joyfd, int sampleRate) {
	
.
.
.
	RTQuaternion deg = sensor->fusionQPose;
.
.
.

	deg = qStart * deg;
	deg.normalize();
.
.
.
	data->angle[0] = deg.x();
	data->angle[1] = deg.y();
	data->angle[2] = deg.z();
	data->angle[3] = deg.scalar();

	data->btnClick = getJoystickDir(joyfd);

}
```


- 최종적으로 컨트롤러의 쿼터니언 회전값, 기기 가속력, Joystick입력값을 포함한 패킷을 초당 10회 샘플링하여 전송

![Untitled](https://github.com/jellypower/PublicImageDataBase/blob/main/Portfolio/UnityRasberryPIVRController/PacketStructure.png)

# 프로젝트 진행 후 피드백

1. 잘 작성된 오픈소스 라이브러리를 학습하며 작업 효율을 높일수 있었고 일반적인 C++라이브러리의 설계 원칙에 대해 학습할 수 있었다.
2. 센서가 측정과 유니티에서 내부적으로 활용하는 Quaternion각도계에 대해 학습하며 제공하는 기능의 학습법을 단순히 학습하는 것이 아닌 수학이론과 내부 동작방식에 대한 이해도가 필요하다는 것을 느낄 수 있었다.
3. 패킷 송수신 블로킹 문제 해결을 위해 쓰레드를 사용했는데, 데이터 수신과 처리를 위한 작업량 자체가 많지 않았기에 논블로킹 소켓을 제작하여 폴링을 진행하는 방식이 더욱 효율적이었을 것이다.
4. 제작 당시 메모리 얼라인먼트에 대한 이해가 부족해 적합한 데이터 교환 구조를 제작하지 못하였다. 때려맞추듯이 데이터 얼라인먼트를 맞추지 않기 위해 데이터 alignment에 대한 추가 학습이 필요해 보인다.
5. 기기 탐색 및 인증 절차를 적법한 근거없이 TCP통신 방식을 따라했었다. 앞으론 단순 모방이 아닌 프로젝트의 스펙에 알맞는 방식으로 프로세스를 규격화할 필요가 있어보인다.
