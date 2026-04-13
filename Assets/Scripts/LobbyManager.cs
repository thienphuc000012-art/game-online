using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public GameObject panneLobby;
    public GameObject panelRoom;

    public TMP_InputField inputPlayerName;
    public Button buttonOk;

    public PlayerClass selectedClass;
    public Image[] classPreviewImage;

    public BasicSpawner spawner;

    public TMP_InputField inputRoomName;
    public Button buttonCreateRoom;
    public GameObject panelRoomList;
    public GameObject roomItemPrefab;

    async void Start()
    {
      

        panneLobby.SetActive(true);
        panelRoom.SetActive(false);

        OnselectedChar(0); // Default to Mage
        buttonOk.onClick.AddListener(OnClickOk);

        buttonCreateRoom.onClick.AddListener(OnClickCreateRoom);

        spawner = FindFirstObjectByType<BasicSpawner>();
        if(spawner != null)
        {
           spawner.LobbyManager = this;
            if (!spawner.IsInlobby  &&  !spawner.IsStartingLobby)
            {
                await spawner.StarLobby();
            }
        }

        await spawner.StarLobby();
    }

    // 0 = Mage, 1 = Archer, 2 = Barbarian, 3 = Elf
    public void OnselectedChar(int classIndex)
    {
        selectedClass = (PlayerClass)classIndex;
        for (var i = 0; i < classPreviewImage.Length; i++)
        {

            classPreviewImage[i].color =(i == classIndex) ? Color.yellow : Color.white;

        }
    }
    public void OnClickOk()
    {
        string playerName = inputPlayerName.text;
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Player name cannot be empty!");
            return;
        }
        var profile = new PlayerClassInfo()
        {
            Name = playerName,
            Class = selectedClass
        };
        // Here you would typically send the player name and selected class to the server
        spawner.SetLocalPlayerProfile(profile);
        panneLobby.SetActive(false);
        panelRoom.SetActive(true);
    }

    public async void OnClickCreateRoom()
    {
        var roomName = inputRoomName.text;
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("Room name cannot be empty!");
            return;
        }
        await  spawner.StartHost(roomName, SceneRef.FromIndex(1)); 
    }
    private readonly List<SessionInfo> _roomEntries = new(); 
    public void UpdateRoomList(List<SessionInfo> sessionList)
    {
        foreach (Transform child in panelRoomList.transform)
        {
            Destroy(child.gameObject);
        }
        _roomEntries.Clear();
        foreach (var session in sessionList)
        {
            var roomItem = Instantiate(roomItemPrefab, panelRoomList.transform);
            roomItem.GetComponentInChildren<TextMeshProUGUI>().text = $"{session.Name} ({session.PlayerCount} / {session.MaxPlayers})";
            roomItem.GetComponentInChildren<Button>().onClick.AddListener(async () =>
            {
                await spawner.StartClient(session.Name);
            }) ;
            roomItemPrefab.gameObject.SetActive(true);

            _roomEntries.Add(session);
        }
    }
}
