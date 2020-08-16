﻿using Discord;
using Facepunch.Steamworks;
using Facepunch.Steamworks.Data;
using MelonLoader;
using StressLevelZero.Pool;
using StressLevelZero.Props.Weapons;
using StressLevelZero.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Object;

using MultiplayerMod.Structs;
using MultiplayerMod.Networking;
using MultiplayerMod.Representations;
using Oculus.Platform;
using Oculus.Platform.Samples.VrHoops;
using BoneworksModdingToolkit.BoneHook;

namespace MultiplayerMod.Core
{
    public class Server
    {
        private readonly Dictionary<byte, PlayerRep> playerObjects = new Dictionary<byte, PlayerRep>(MultiplayerMod.MAX_PLAYERS);
        private readonly Dictionary<byte, string> playerNames = new Dictionary<byte, string>(MultiplayerMod.MAX_PLAYERS);
        private readonly List<ulong> players = new List<ulong>();
        private readonly Dictionary<SteamId, byte> smallPlayerIds = new Dictionary<SteamId, byte>(MultiplayerMod.MAX_PLAYERS);
        private readonly Dictionary<byte, SteamId> largePlayerIds = new Dictionary<byte, SteamId>(MultiplayerMod.MAX_PLAYERS);
        private readonly Dictionary<ulong, ITransportConnection> playerConnections = new Dictionary<ulong, ITransportConnection>(MultiplayerMod.MAX_PLAYERS);
        private readonly EnemyPoolManager enemyPoolManager = new EnemyPoolManager();
        private string partyId = "";
        private byte smallIdCounter = 0;
        private BoneworksRigTransforms localRigTransforms;
        private readonly MultiplayerUI ui;
        private readonly ITransportLayer transportLayer;

        public bool IsRunning { get; private set; }

        public Server(MultiplayerUI ui, ITransportLayer transportLayer)
        {
            this.ui = ui;
            this.transportLayer = transportLayer;
            PlayerHooks.OnPlayerDeath += OnDeath;
        }

        public void Update()
        {
            if (SceneLoader.loading) return;
            //ui.SetPlayerCount(players.Count);

            #region Unused Code
            //if (localHead != null && !enableFullRig)
            //{
            //    OtherPlayerPositionMessage oppm = new OtherPlayerPositionMessage
            //    {
            //        playerId = 0,
            //        headPos = localHead.transform.position,
            //        lHandPos = localHandL.transform.position,
            //        rHandPos = localHandR.transform.position,
            //        pelvisPos = localPelvis.transform.position,
            //        lFootPos = localFootL.transform.position,
            //        rFootPos = localFootR.transform.position,

            //        headRot = localHead.transform.rotation,
            //        lHandRot = localHandL.transform.rotation,
            //        rHandRot = localHandR.transform.rotation,
            //        pelvisRot = localPelvis.transform.rotation,
            //        lFootRot = localFootL.transform.rotation,
            //        rFootRot = localFootR.transform.rotation
            //    };

            //    ServerSendToAll(oppm, P2PSend.Unreliable);
            //} 
            //else if(enableFullRig && localHead != null)
            #endregion

            if (localRigTransforms.main == null)
                localRigTransforms = BWUtil.GetLocalRigTransforms();

            if (localRigTransforms.main != null)
            {
                OtherFullRigTransformMessage ofrtm = new OtherFullRigTransformMessage
                {
                    playerId = 0,
                    posMain = localRigTransforms.main.position,
                    posRoot = localRigTransforms.root.position,
                    posLHip = localRigTransforms.lHip.position,
                    posRHip = localRigTransforms.rHip.position,
                    posLKnee = localRigTransforms.lKnee.position,
                    posRKnee = localRigTransforms.rKnee.position,
                    posLAnkle = localRigTransforms.lAnkle.position,
                    posRAnkle = localRigTransforms.rAnkle.position,

                    posSpine1 = localRigTransforms.spine1.position,
                    posSpine2 = localRigTransforms.spine2.position,
                    posSpineTop = localRigTransforms.spineTop.position,
                    posLClavicle = localRigTransforms.lClavicle.position,
                    posRClavicle = localRigTransforms.rClavicle.position,
                    posNeck = localRigTransforms.neck.position,
                    posLShoulder = localRigTransforms.lShoulder.position,
                    posRShoulder = localRigTransforms.rShoulder.position,
                    posLElbow = localRigTransforms.lElbow.position,
                    posRElbow = localRigTransforms.rElbow.position,
                    posLWrist = localRigTransforms.lWrist.position,
                    posRWrist = localRigTransforms.rWrist.position,

                    rotMain = localRigTransforms.main.rotation,
                    rotRoot = localRigTransforms.root.rotation,
                    rotLHip = localRigTransforms.lHip.rotation,
                    rotRHip = localRigTransforms.rHip.rotation,
                    rotLKnee = localRigTransforms.lKnee.rotation,
                    rotRKnee = localRigTransforms.rKnee.rotation,
                    rotLAnkle = localRigTransforms.lAnkle.rotation,
                    rotRAnkle = localRigTransforms.rAnkle.rotation,
                    rotSpine1 = localRigTransforms.spine1.rotation,
                    rotSpine2 = localRigTransforms.spine2.rotation,
                    rotSpineTop = localRigTransforms.spineTop.rotation,
                    rotLClavicle = localRigTransforms.lClavicle.rotation,
                    rotRClavicle = localRigTransforms.rClavicle.rotation,
                    rotNeck = localRigTransforms.neck.rotation,
                    rotLShoulder = localRigTransforms.lShoulder.rotation,
                    rotRShoulder = localRigTransforms.rShoulder.rotation,
                    rotLElbow = localRigTransforms.lElbow.rotation,
                    rotRElbow = localRigTransforms.rElbow.rotation,
                    rotLWrist = localRigTransforms.lWrist.rotation,
                    rotRWrist = localRigTransforms.rWrist.rotation
                };

                ServerSendToAll(ofrtm, MessageSendType.Unreliable);
            }

            foreach (PlayerRep pr in playerObjects.Values)
            {
                pr.UpdateNameplateFacing(Camera.current.transform);
            }
            // Disabled temporarily
#if false
            {
                enemyPoolManager.FindMissingPools();
                Pool pool = enemyPoolManager.GetPool(EnemyType.NullBody);
                for (int i = 0; i < pool.transform.childCount; i++)
                {
                    GameObject childEnemy = pool.transform.GetChild(i).gameObject;

                    BoneworksRigTransforms brt = BWUtil.GetHumanoidRigTransforms(childEnemy.transform.Find("brettEnemy@neutral").gameObject);

                    EnemyRigTransformMessage ertf = new EnemyRigTransformMessage()
                    {
                        poolChildIdx = (byte)i,
                        enemyType = EnemyType.NullBody,
                        posMain = localRigTransforms.main.position,
                        posRoot = localRigTransforms.root.position,
                        posLHip = localRigTransforms.lHip.position,
                        posRHip = localRigTransforms.rHip.position,
                        posLKnee = localRigTransforms.lKnee.position,
                        posRKnee = localRigTransforms.rKnee.position,
                        posLAnkle = localRigTransforms.lAnkle.position,
                        posRAnkle = localRigTransforms.rAnkle.position,

                        posSpine1 = localRigTransforms.spine1.position,
                        posSpine2 = localRigTransforms.spine2.position,
                        posSpineTop = localRigTransforms.spineTop.position,
                        posLClavicle = localRigTransforms.lClavicle.position,
                        posRClavicle = localRigTransforms.rClavicle.position,
                        posNeck = localRigTransforms.neck.position,
                        posLShoulder = localRigTransforms.lShoulder.position,
                        posRShoulder = localRigTransforms.rShoulder.position,
                        posLElbow = localRigTransforms.lElbow.position,
                        posRElbow = localRigTransforms.rElbow.position,
                        posLWrist = localRigTransforms.lWrist.position,
                        posRWrist = localRigTransforms.rWrist.position,

                        rotMain = localRigTransforms.main.rotation,
                        rotRoot = localRigTransforms.root.rotation,
                        rotLHip = localRigTransforms.lHip.rotation,
                        rotRHip = localRigTransforms.rHip.rotation,
                        rotLKnee = localRigTransforms.lKnee.rotation,
                        rotRKnee = localRigTransforms.rKnee.rotation,
                        rotLAnkle = localRigTransforms.lAnkle.rotation,
                        rotRAnkle = localRigTransforms.rAnkle.rotation,
                        rotSpine1 = localRigTransforms.spine1.rotation,
                        rotSpine2 = localRigTransforms.spine2.rotation,
                        rotSpineTop = localRigTransforms.spineTop.rotation,
                        rotLClavicle = localRigTransforms.lClavicle.rotation,
                        rotRClavicle = localRigTransforms.rClavicle.rotation,
                        rotNeck = localRigTransforms.neck.rotation,
                        rotLShoulder = localRigTransforms.lShoulder.rotation,
                        rotRShoulder = localRigTransforms.rShoulder.rotation,
                        rotLElbow = localRigTransforms.lElbow.rotation,
                        rotRElbow = localRigTransforms.rElbow.rotation,
                        rotLWrist = localRigTransforms.lWrist.rotation,
                        rotRWrist = localRigTransforms.rWrist.rotation
                    };

                    ServerSendToAll(ertf, P2PSend.UnreliableNoDelay);
                }
            }
#endif
        }

        private void MultiplayerMod_OnLevelWasLoadedEvent(int level)
        {
            SceneTransitionMessage stm = new SceneTransitionMessage
            {
                sceneName = BoneworksSceneManager.GetSceneNameFromScenePath(level)
            };
            ServerSendToAll(stm, MessageSendType.Reliable);
            enemyPoolManager.FindAllPools();
        }

        private void OnDeath()
        {
            Death deathMessage = new Death()
            {
                player = SteamClient.Name
            };

            MelonLogger.Log(deathMessage.player + " has died");

            ServerSendToAll(deathMessage, MessageSendType.Reliable);
        }

        public void StartServer()
        {
            ui.SetState(MultiplayerUIState.Server);
            MelonModLogger.Log("Starting server...");
            localRigTransforms = BWUtil.GetLocalRigTransforms();
            partyId = SteamClient.SteamId + "P" + DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            RichPresence.SetActivity(
                new Activity()
                {
                    Details = "Hosting a server",
                    Secrets = new ActivitySecrets()
                    {
                        Join = SteamClient.SteamId.ToString()
                    },
                    Party = new ActivityParty()
                    {
                        Id = partyId,
                        Size = new PartySize()
                        {
                            CurrentSize = 1,
                            MaxSize = MultiplayerMod.MAX_PLAYERS
                        }
                    }
                });
            transportLayer.OnMessageReceived += TransportLayer_OnMessageReceived;
            transportLayer.OnConnectionClosed += TransportLayer_OnConnectionClosed;

            MultiplayerMod.OnLevelWasLoadedEvent += MultiplayerMod_OnLevelWasLoadedEvent;

            IsRunning = true;
        }

        private void TransportLayer_OnConnectionClosed(ITransportConnection connection, ConnectionClosedReason reason)
        {
            switch (reason)
            {
                case ConnectionClosedReason.Timeout:
                case ConnectionClosedReason.ClosedByRemote:
                    if (smallPlayerIds.ContainsKey(connection.ConnectedTo))
                    {
                        MelonModLogger.Log("Player left with ID: " + connection.ConnectedTo);
                        byte smallId = smallPlayerIds[connection.ConnectedTo];

                        P2PMessage disconnectMsg = new P2PMessage();
                        disconnectMsg.WriteByte((byte)MessageType.Disconnect);
                        disconnectMsg.WriteByte(smallId);

                        playerObjects[smallId].Destroy();
                        playerObjects.Remove(smallId);
                        players.RemoveAll((ulong val) => val == connection.ConnectedTo);
                        smallPlayerIds.Remove(connection.ConnectedTo);

                        foreach (SteamId p in players)
                        {
                            playerConnections[p].SendMessage(disconnectMsg, MessageSendType.Reliable);
                        }
                    }
                    break;
                case ConnectionClosedReason.Other:
                    break;
                
            }
        }

        private void TransportLayer_OnMessageReceived(ITransportConnection connection, P2PMessage msg)
        {
            MessageType type = (MessageType)msg.ReadByte();

            switch (type)
            {
                case MessageType.Join:
                    {
                        if (msg.ReadByte() != MultiplayerMod.PROTOCOL_VERSION)
                        {
                            // Somebody tried to join with an incompatible verison
                            P2PMessage m2 = new P2PMessage();
                            m2.WriteByte((byte)MessageType.JoinRejected);
                            connection.SendMessage(m2, MessageSendType.Reliable);
                            connection.Disconnect();
                        }
                        else
                        {
                            MelonModLogger.Log("Player joined with ID: " + connection.ConnectedTo);
                            players.Add(connection.ConnectedTo);
                            MelonModLogger.Log("Player count: " + players.Count);
                            byte newPlayerId = smallIdCounter;
                            smallPlayerIds.Add(connection.ConnectedTo, newPlayerId);
                            largePlayerIds.Add(newPlayerId, connection.ConnectedTo);
                            smallIdCounter++;

                            playerConnections.Add(connection.ConnectedTo, connection);

                            string name = msg.ReadUnicodeString();
                            MelonModLogger.Log("Name: " + name);

                            foreach (var smallId in playerNames.Keys)
                            {
                                ClientJoinMessage cjm = new ClientJoinMessage
                                {
                                    playerId = smallId,
                                    name = playerNames[smallId],
                                    steamId = largePlayerIds[smallId]
                                };
                                connection.SendMessage(cjm.MakeMsg(), MessageSendType.Reliable);
                            }

                            ClientJoinMessage cjm2 = new ClientJoinMessage
                            {
                                playerId = 0,
                                name = SteamClient.Name,
                                steamId = SteamClient.SteamId
                            };
                            connection.SendMessage(cjm2.MakeMsg(), MessageSendType.Reliable);

                            playerNames.Add(newPlayerId, name);

                            ClientJoinMessage cjm3 = new ClientJoinMessage
                            {
                                playerId = newPlayerId,
                                name = name,
                                steamId = connection.ConnectedTo
                            };
                            ServerSendToAllExcept(cjm3, MessageSendType.Reliable, connection.ConnectedTo);

                            playerObjects.Add(newPlayerId, new PlayerRep(name, connection.ConnectedTo));

                            RichPresence.SetActivity(
                                new Activity()
                                {
                                    Details = "Hosting a server",
                                    Secrets = new ActivitySecrets()
                                    {
                                        Join = SteamClient.SteamId.ToString()
                                    },
                                    Party = new ActivityParty()
                                    {
                                        Id = partyId,
                                        Size = new PartySize()
                                        {
                                            CurrentSize = players.Count + 1,
                                            MaxSize = MultiplayerMod.MAX_PLAYERS
                                        }
                                    }
                                });

                            SceneTransitionMessage stm = new SceneTransitionMessage()
                            {
                                sceneName = BoneworksSceneManager.GetCurrentSceneName()
                            };
                            connection.SendMessage(stm.MakeMsg(), MessageSendType.Reliable);

                            SetPartyIdMessage spid = new SetPartyIdMessage()
                            {
                                partyId = partyId
                            };
                            connection.SendMessage(spid.MakeMsg(), MessageSendType.Reliable);

                            ui.SetPlayerCount(players.Count, MultiplayerUIState.Server);
                        }
                        break;
                    }
                case MessageType.Disconnect:
                    {
                        MelonModLogger.Log("Player left with ID: " + connection.ConnectedTo);
                        byte smallId = smallPlayerIds[connection.ConnectedTo];

                        playerObjects[smallId].Destroy();
                        playerObjects.Remove(smallId);
                        players.RemoveAll((ulong val) => val == connection.ConnectedTo);
                        smallPlayerIds.Remove(connection.ConnectedTo);

                        P2PMessage disconnectMsg = new P2PMessage();
                        disconnectMsg.WriteByte((byte)MessageType.Disconnect);
                        disconnectMsg.WriteByte(smallId);

                        foreach (SteamId p in players)
                        {
                            playerConnections[p].SendMessage(disconnectMsg, MessageSendType.Reliable);
                        }
                        break;
                    }
                //case MessageType.PlayerPosition:
                //    {
                //        if (smallPlayerIds.ContainsKey(packet.Value.SteamId))
                //        {
                //            byte playerId = smallPlayerIds[packet.Value.SteamId];
                //            PlayerRep pr = GetPlayerRep(playerId);

                //            PlayerPositionMessage ppm = new PlayerPositionMessage(msg);
                //            pr.head.transform.position = ppm.headPos;
                //            pr.handL.transform.position = ppm.lHandPos;
                //            pr.handR.transform.position = ppm.rHandPos;
                //            pr.ford.transform.position = ppm.pelvisPos - new Vector3(0.0f, 0.3f, 0.0f);
                //            pr.pelvis.transform.position = ppm.pelvisPos;
                //            pr.footL.transform.position = ppm.lFootPos;
                //            pr.footR.transform.position = ppm.rFootPos;

                //            //pr.ford.transform.rotation = ppm.pelvisRot;
                //            pr.head.transform.rotation = ppm.headRot;
                //            pr.handL.transform.rotation = ppm.lHandRot;
                //            pr.handR.transform.rotation = ppm.rHandRot;
                //            pr.pelvis.transform.rotation = ppm.pelvisRot;
                //            pr.footL.transform.rotation = ppm.lFootRot;
                //            pr.footR.transform.rotation = ppm.rFootRot;

                //            // Send to all other players

                //            OtherPlayerPositionMessage relayOPPM = new OtherPlayerPositionMessage
                //            {
                //                headPos = ppm.headPos,
                //                lHandPos = ppm.lHandPos,
                //                rHandPos = ppm.rHandPos,
                //                pelvisPos = ppm.pelvisPos,
                //                lFootPos = ppm.lFootPos,
                //                rFootPos = ppm.rFootPos,

                //                headRot = ppm.headRot,
                //                lHandRot = ppm.lHandRot,
                //                rHandRot = ppm.rHandRot,
                //                pelvisRot = ppm.pelvisRot,
                //                lFootRot = ppm.lFootRot,
                //                rFootRot = ppm.rFootRot,
                //                playerId = smallPlayerIds[packet.Value.SteamId]
                //            };

                //            ServerSendToAllExcept(relayOPPM, P2PSend.Unreliable, packet.Value.SteamId);
                //        }
                //        break;
                //    }
                case MessageType.FullRig:
                    {
                        FullRigTransformMessage frtm = new FullRigTransformMessage(msg);

                        byte playerId = smallPlayerIds[connection.ConnectedTo];
                        if (playerObjects.ContainsKey(playerId))
                        {
                            PlayerRep pr = playerObjects[playerId];

                            if (pr.rigTransforms.main != null)
                            {
                                //ApplyTransformMessage(pr, frtm);
                                pr.ApplyTransformMessage(frtm);

                                OtherFullRigTransformMessage ofrtm = new OtherFullRigTransformMessage
                                {
                                    playerId = playerId,

                                    posMain = frtm.posMain,
                                    posRoot = frtm.posRoot,
                                    posLHip = frtm.posLHip,
                                    posRHip = frtm.posRHip,
                                    posLKnee = frtm.posLKnee,
                                    posRKnee = frtm.posRKnee,
                                    posLAnkle = frtm.posLAnkle,
                                    posRAnkle = frtm.posRAnkle,

                                    posSpine1 = frtm.posSpine1,
                                    posSpine2 = frtm.posSpine2,
                                    posSpineTop = frtm.posSpineTop,
                                    posLClavicle = frtm.posLClavicle,
                                    posRClavicle = frtm.posRClavicle,
                                    posNeck = frtm.posNeck,
                                    posLShoulder = frtm.posLShoulder,
                                    posRShoulder = frtm.posRShoulder,
                                    posLElbow = frtm.posLElbow,
                                    posRElbow = frtm.posRElbow,
                                    posLWrist = frtm.posLWrist,
                                    posRWrist = frtm.posRWrist,

                                    rotMain = frtm.rotMain,
                                    rotRoot = frtm.rotRoot,
                                    rotLHip = frtm.rotLHip,
                                    rotRHip = frtm.rotRHip,
                                    rotLKnee = frtm.rotLKnee,
                                    rotRKnee = frtm.rotRKnee,
                                    rotLAnkle = frtm.rotLAnkle,
                                    rotRAnkle = frtm.rotRAnkle,
                                    rotSpine1 = frtm.rotSpine1,
                                    rotSpine2 = frtm.rotSpine2,
                                    rotSpineTop = frtm.rotSpineTop,
                                    rotLClavicle = frtm.rotLClavicle,
                                    rotRClavicle = frtm.rotRClavicle,
                                    rotNeck = frtm.rotNeck,
                                    rotLShoulder = frtm.rotLShoulder,
                                    rotRShoulder = frtm.rotRShoulder,
                                    rotLElbow = frtm.rotLElbow,
                                    rotRElbow = frtm.rotRElbow,
                                    rotLWrist = frtm.rotLWrist,
                                    rotRWrist = frtm.rotRWrist
                                };

                                ServerSendToAllExcept(ofrtm, MessageSendType.Unreliable, connection.ConnectedTo);
                            }
                        }
                        break;
                    }
                case MessageType.HandGunChange:
                    {
                        HandGunChangeMessage hgcm = new HandGunChangeMessage(msg, false)
                        {
                            playerId = smallPlayerIds[connection.ConnectedTo],
                            isForOtherPlayer = false
                        };

                        MelonModLogger.Log("Got HGC: " + hgcm.type.ToString() + ", destroy: " + hgcm.destroy.ToString());

                        if (hgcm.destroy)
                        {
                            Destroy(playerObjects[smallPlayerIds[connection.ConnectedTo]].currentGun);
                        }
                        else
                        {
                            MelonModLogger.Log("Spawning " + hgcm.type.ToString());
                            PlayerRep pr = playerObjects[smallPlayerIds[connection.ConnectedTo]];
                            pr.currentGun = BWUtil.SpawnGun(hgcm.type);
                            if (pr.currentGun == null)
                                MelonModLogger.LogError("Failed to spawn gun");
                            pr.currentGun.transform.parent = pr.gunParent.transform;
                            pr.currentGun.transform.localPosition = Vector3.zero;
                            pr.currentGun.transform.localRotation = Quaternion.identity;//Quaternion.AngleAxis(90.0f, new Vector3(0.0f, 1.0f, 0.0f)) * Quaternion.AngleAxis(90.0f, new Vector3(1.0f, 0.0f, 0.0f));
                            pr.currentGun.GetComponentInChildren<Rigidbody>().isKinematic = true;
                        }

                        hgcm.isForOtherPlayer = true;
                        ServerSendToAllExcept(hgcm, MessageSendType.Reliable, connection.ConnectedTo);
                        break;
                    }
                case MessageType.Death:
                    {
                        Death deathMessage = new Death(msg)
                        {
                            player = playerNames[smallPlayerIds[connection.ConnectedTo]]
                        };

                        MelonLogger.Log(deathMessage.player + " has died");
                        ServerSendToAllExcept(deathMessage, MessageSendType.Reliable, connection.ConnectedTo);
                        break;
                    }
                default:
                    MelonModLogger.Log("Unknown message type: " + type.ToString());
                    break;
            }
        }

        public void StopServer()
        {
            ui.SetState(MultiplayerUIState.PreConnect);
            IsRunning = false;

            try
            {
                foreach (PlayerRep r in playerObjects.Values)
                {
                    r.Destroy();
                }
            }
            catch (Exception)
            {
                MelonModLogger.LogError("Caught exception destroying player objects");
            }

            playerObjects.Clear();
            playerNames.Clear();
            smallPlayerIds.Clear();
            largePlayerIds.Clear();
            smallIdCounter = 1;

            P2PMessage shutdownMsg = new P2PMessage();
            shutdownMsg.WriteByte((byte)MessageType.ServerShutdown);

            foreach (SteamId p in players)
            {
                playerConnections[p].SendMessage(shutdownMsg, MessageSendType.Reliable);
                playerConnections[p].Disconnect();
            }

            players.Clear();

            transportLayer.OnMessageReceived -= TransportLayer_OnMessageReceived;
            transportLayer.OnConnectionClosed -= TransportLayer_OnConnectionClosed;

            MultiplayerMod.OnLevelWasLoadedEvent -= MultiplayerMod_OnLevelWasLoadedEvent;
        }

        private void ServerSendToAll(INetworkMessage msg, MessageSendType send)
        {
            P2PMessage pMsg = msg.MakeMsg();
            foreach (SteamId p in players)
            {
                playerConnections[p].SendMessage(pMsg, send);
            }
        }

        private void ServerSendToAllExcept(INetworkMessage msg, MessageSendType send, SteamId except)
        {
            P2PMessage pMsg = msg.MakeMsg();
            foreach (SteamId p in players)
            {
                if (p != except)
                    playerConnections[p].SendMessage(pMsg, send);
            }
        }

        private void SendToId(INetworkMessage msg, MessageSendType send, SteamId id)
        {
            P2PMessage pMsg = msg.MakeMsg();
            playerConnections[id].SendMessage(pMsg, send);
        }

        private void PlayerHooks_OnPlayerLetGoObject(GameObject obj)
        {
            HandGunChangeMessage hgcm = new HandGunChangeMessage()
            {
                isForOtherPlayer = true,
                destroy = true,
                playerId = 0
            };

            ServerSendToAll(hgcm, MessageSendType.Reliable);
        }

        private void PlayerHooks_OnPlayerGrabObject(GameObject obj)
        {
            // See if it's a gun
            GunType? gt = BWUtil.GetGunType(obj.transform.root.gameObject);
            if (gt != null)
            {
                HandGunChangeMessage hgcm = new HandGunChangeMessage()
                {
                    isForOtherPlayer = true,
                    type = gt.Value,
                    destroy = false,
                    playerId = 0
                };

                ServerSendToAll(hgcm, MessageSendType.Reliable);

                switch (gt)
                {
                    case GunType.EDER22:
                        MelonModLogger.Log("Holding Eder22");
                        break;
                }
            }
        }
    }
}
