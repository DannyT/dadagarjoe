﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;
    public float RoomWidth = 7f;
    public float RoomDepth = 7f;

    public float CurrentRoomX = 0;
    public float CurrentRoomZ = 0;

    public Room[] Choices = new Room[2];
    public Room PreviousRoom;
    public Room CurrentRoom;

    public GameObject RoomPrefab;

    public RenderTexture roomART;
    public RenderTexture roomBRT;

    public int Seed;


    List<Room> oldRooms = new List<Room>();

    bool hasSentFirstRooms = false;
    bool hasAskedForResults = false;

	// Use this for initialization
	void Start ()
	{
	    Instance = this;

	    Seed = (int) Time.realtimeSinceStartup;
        Random.InitState(Seed);

	    GenerateChoices();
	    SpawnNext(0);

	  
    }

    // Update is called once per frame
	void Update () {
	  // if(GetComponent<SocketController>().connection.IsActive && !hasSentFirstRooms)
         //   TakeSnapshots();
	    if (!hasAskedForResults && CurrentRoom.IsComplete)
	    {
	        hasAskedForResults = true;
            Debug.Log("asked for result");
            GetComponent<SocketController>().proxy.Invoke("GetVotedForRoom");
	    }
	}

    public void SpawnNext(int choice)
    {
        hasAskedForResults = false;
        //if(PreviousRoom!=null) Destroy(PreviousRoom.gameObject);
        if (CurrentRoom != null) PreviousRoom = CurrentRoom;
        CurrentRoom = Choices[choice];

        if (PreviousRoom != null)
        {
            switch (PreviousRoom.Exit)
            {
                case Room.Direction.North:
                    CurrentRoomZ += RoomDepth;
                    break;
                case Room.Direction.South:
                    CurrentRoomZ -= RoomDepth;
                    break;
                case Room.Direction.East:
                    CurrentRoomX += RoomWidth;

                    break;
                case Room.Direction.West:
                    CurrentRoomX -= RoomWidth;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else CurrentRoom.Doors[(int)CurrentRoom.Entrance].SetState(Door.DoorState.Wall);

        CurrentRoom.transform.position = new Vector3(CurrentRoomX,0f,CurrentRoomZ);
        CurrentRoom.IsActive = true;
        if (PreviousRoom != null) PreviousRoom.CanExit = true;

        oldRooms.Add(CurrentRoom);
        if (oldRooms.Count > 3)
        {
            Destroy(oldRooms[0].gameObject);
            oldRooms.RemoveAt(0);
        }

        GenerateChoices();
    }

    private void GenerateChoices()
    {
        if (Choices[0] != null && !Choices[0].IsActive) Destroy(Choices[0].gameObject);
        if (Choices[1] != null && !Choices[1].IsActive) Destroy(Choices[1].gameObject);

        Choices[0] = ((GameObject) Instantiate(RoomPrefab, new Vector3(-100, -100, 0), Quaternion.identity)).GetComponent<Room>();
        Choices[1] = ((GameObject) Instantiate(RoomPrefab, new Vector3(100, -100, 0), Quaternion.identity)).GetComponent<Room>();
        Choices[0].transform.SetParent(transform);
        Choices[1].transform.SetParent(transform);

        var prevExit = Room.Direction.South;
        if (CurrentRoom != null) prevExit = CurrentRoom.Exit;
        Choices[0].Roll(prevExit);
        Choices[1].Roll(prevExit);

        Invoke("TakeSnapshots" , 0.1f);
    }

    void TakeSnapshots()
    {
        if (!GetComponent<SocketController>().connection.IsActive)
        {
            Invoke("TakeSnapshots", 0.1f);
            return;

        }


        RenderTexture.active = roomART;
        Texture2D virtualPhoto = new Texture2D(roomART.width, roomART.height, TextureFormat.RGB24, false);
        virtualPhoto.ReadPixels(new Rect(0, 0, roomART.width, roomART.height), 0, 0);
        byte[] roomA = virtualPhoto.EncodeToJPG();
        var rm1i = "data:image/jpg;base64," + Convert.ToBase64String(roomA);
        

        RenderTexture.active = roomBRT;
        virtualPhoto.ReadPixels(new Rect(0, 0, roomBRT.width, roomBRT.height), 0, 0);
        byte[] roomB = virtualPhoto.EncodeToJPG();
        var rm2i = "data:image/jpg;base64," + Convert.ToBase64String(roomB);

        var dto1 = new RoomDto()
        {
            EntryLocation = (int) Choices[0].Entrance,
            ExitLocation = (int) Choices[0].Exit,
            HasReward = false,
            RoomName = "RoomA"
        };
        var dto2 = new RoomDto()
        {
            EntryLocation = (int)Choices[1].Entrance,
            ExitLocation = (int)Choices[1].Exit,
            HasReward = false,
            RoomName = "RoomB"
        };

        // Send the choices to SignalR here
       // GetComponent<SocketController>().Init(dto1, dto2, rm1i, rm2i);
      // if(hasSentFirstRooms)
            GetComponent<SocketController>().SendRooms(dto1, dto2, rm1i, rm2i);

        hasSentFirstRooms = true;


        //System.IO.File.WriteAllBytes("d:\\" + Guid.NewGuid() + ".png", roomA);

    }

    //public bool CanPlaceRoom(Room.Direction exit)
    //{
    //    float roomModX = 0f;
    //    float roomModZ = 0f;
    //    switch (exit)
    //    {
    //        case Room.Direction.North:
    //            roomModZ += RoomDepth;
    //            break;
    //        case Room.Direction.South:
    //            roomModZ -= RoomDepth;
    //            break;
    //        case Room.Direction.East:
    //            roomModX += RoomWidth;
    //            break;
    //        case Room.Direction.West:
    //            roomModX -= RoomWidth;
    //            break;
    //        default:
    //            throw new ArgumentOutOfRangeException();
    //    }

    //    return !oldRooms.Contains(new Vector2(CurrentRoomX + roomModX, CurrentRoomZ + roomModZ));
    //}
}
