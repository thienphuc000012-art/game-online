using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class GameSceneManager : NetworkBehaviour
{

    public Button buttonQuitRoom;
    public BasicSpawner spawner;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        buttonQuitRoom.onClick.RemoveAllListeners();
        buttonQuitRoom.onClick.AddListener(OnClickQuitRoom);
    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnClickQuitRoom()
    {
        if (spawner == null)
            spawner = FindFirstObjectByType<BasicSpawner>();

        if (spawner != null)
            _ = spawner.LeaveRoomAndReturnToLobby(); // gọi đúng tên hàm
    }
}
