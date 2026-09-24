using UnityEngine;

namespace KinectKids3D
{
    public sealed partial class DarkRideWorld
    {
        private void BuildEntranceHall()
        {
            for (float z = 5; z < 48; z += 9f)
            {
                CreateArch(z, amberGlow);
                CreateTorch(z + 2.2f, -1);
                CreateTorch(z + 6.0f, 1);
            }
            CreateCastleGate(8f);
            CreateSign(14f, "SPÖKJAKTEN", amberGlow);
            CreateArmor(22f, -1);
            CreateArmor(31f, 1);
            WallDoorScare.Create(38f, -1, 0, wood, darkStone, rail);
            CreateBatSwarm(25f, -2.6f, 3.35f);
            CreateBatSwarm(44f, 2.4f, 2.75f);
            CreateCeilingSpider(33f, 1, 0);
            CreateCobweb(20f, -1);
            CreateWatchingPortrait(36f, 1);
            BreakableProp.Create(29f, 3.8f, 0, BreakablePropKind.Crate);
            BreakableProp.Create(41f, -3.9f, 0, BreakablePropKind.Portrait);
        }

        private void BuildCrypt()
        {
            for (float z = 52; z < 105; z += 8f)
            {
                CreateArch(z, greenGlow);
                CreateTorch(z + 2.6f, z % 16f < 1f ? -1 : 1);
                float center = TrackCenter(z);
                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject grave = CreateGravestone(z + 2f, side, 0, (int)z + side);
                    CreateSphere("Spökeld", grave.transform.position + Vector3.up * 0.95f, Vector3.one * 0.24f, greenGlow);
                }
            }
            CreateBatSwarm(63f, 2.2f, 3.2f);
            CreateGrabbingHands(86f, -2.7f, 0);
            CreateCobweb(59f, 1);
            CreateCobweb(89f, -1);
            CreateWatchingPortrait(77f, -1);
            StalkingMonster.Create(74f, -1, 0, 0);
            CreateArmor(84f, 1);
            WallDoorScare.Create(94f, 1, 0, wood, darkStone, rail);
            CreateGraveyardProp("Models/KenneyGraveyard/coffin-old", 71f, -1, 0, 2.25f, 8f);
            CreateGraveyardProp("Models/KenneyGraveyard/altar-stone", 98f, 1, 0, 2.1f, -7f);
            BreakableProp.Create(68f, 3.7f, 0, BreakablePropKind.Urn);
            BreakableProp.Create(101f, -3.6f, 0, BreakablePropKind.Urn);
            BreakableProp.Create(88f, 3.9f, 0, BreakablePropKind.Crate);
            CreateCastleGate(103f);
        }

        private void BuildCastleCourtyard()
        {
            for (float z = 108; z < 146; z += 9f)
            {
                CreateBattlements(z);
                CreateTorch(z + 2.4f, -1);
                CreateTorch(z + 5.8f, 1);
            }
            CreateCastleGate(109f);
            CreateCastleTower(118f, -1);
            CreateCastleTower(132f, 1);
            CreateArmor(116f, 1);
            CreateArmor(137f, -1);
            WallDoorScare.Create(126f, -1, 0, wood, darkStone, rail);
            CreateBatSwarm(128f, -2.3f, 3.65f);
            CreateCeilingSpider(141f, -1, 0);
            CreateHangingChain(119f, -3.7f);
            CreateHangingChain(138f, 3.8f);
            BreakableProp.Create(133f, 3.6f, 0, BreakablePropKind.Crate);
            BreakableProp.Create(146f, -3.8f, 0, BreakablePropKind.Urn);
        }

        private void BuildHauntedGallery()
        {
            CreateCastleGate(149f);
            for (float z = 154f; z < 199f; z += 10f)
            {
                CreateArch(z, purpleGlow);
                if (((int)z / 10) % 2 == 0) CreateTorch(z + 2.4f, -1);
                CreateWatchingPortrait(z + 4.2f, ((int)z / 10) % 2 == 0 ? 1 : -1);
            }
            CreateArmor(166f, -1);
            CreateArmor(183f, 1);
            HiddenMonster.Create(161f, 1, 0, HiddenMonsterKind.Portrait);
            HiddenMonster.Create(179f, -1, 0, HiddenMonsterKind.Cabinet);
            HiddenMonster.Create(196f, 1, 0, HiddenMonsterKind.Portrait);
            WallDoorScare.Create(174f, 1, 0, wood, darkStone, rail);
            CreateWatchingPortrait(190f, -1);
            MirrorScare.Create(156f, 1, 0);
            StalkingMonster.Create(187f, 1, 0, 1);
            CreateMist(158f, 0);
            CreateMist(188f, 0);
            BreakableProp.Create(172f, -4.4f, 0, BreakablePropKind.Portrait);
            BreakableProp.Create(193f, 4.2f, 0, BreakablePropKind.Portrait);
        }

        private void BuildForkedPassages()
        {
            CreateCastleGate(201f);
            float forkX = TrackCenter(205f);
            CreateSphere("Vänster vägvisare", new Vector3(forkX - 2.4f, 2.5f, 205f),
                Vector3.one * 0.34f, greenGlow);
            CreateSphere("Höger vägvisare", new Vector3(forkX + 2.4f, 2.5f, 205f),
                Vector3.one * 0.34f, purpleGlow);

            RouteDoor.Create(216f, -1, wood, rail, greenGlow).transform.SetParent(root, true);
            RouteDoor.Create(216f, 1, wood, rail, purpleGlow).transform.SetParent(root, true);

            for (int route = -1; route <= 1; route += 2)
            {
                for (float z = 214f; z < 300f; z += 11f)
                {
                    CreateArch(z, route < 0 ? greenGlow : purpleGlow, route);
                    if (((int)z / 11) % 2 == 0) CreateTorch(z + 2.3f, route < 0 ? -1 : 1, route);
                }
                CreateArmor(228f, route < 0 ? -1 : 1, route);
                CreateCobweb(246f, route < 0 ? 1 : -1, route);
                CreateHangingChain(266f, route < 0 ? -3.6f : 3.6f, route);
                CreateWatchingPortrait(284f, route < 0 ? -1 : 1, route);
                HiddenMonster.Create(235f, route < 0 ? 1 : -1, route, HiddenMonsterKind.Cabinet);
                HiddenMonster.Create(258f, route < 0 ? -1 : 1, route, HiddenMonsterKind.Tomb);
                HiddenMonster.Create(289f, route < 0 ? 1 : -1, route, HiddenMonsterKind.Portrait);
                WallDoorScare.Create(route < 0 ? 250f : 279f, route < 0 ? -1 : 1, route,
                    wood, darkStone, rail);
                CreateGraveyardProp(route < 0 ? "Models/KenneyGraveyard/urn-round" : "Models/KenneyGraveyard/pumpkin-carved",
                    244f, route < 0 ? 1 : -1, route, 1.25f, route * 12f);
                CreateMist(222f, route);
                CreateMist(274f, route);
                CreateCeilingSpider(route < 0 ? 262f : 246f, route < 0 ? 1 : -1, route);
                CreateGrabbingHands(route < 0 ? 291f : 268f, route < 0 ? -2.5f : 2.5f, route);
                BreakableProp.Create(route < 0 ? 232f : 272f, route < 0 ? 3.7f : -3.7f,
                    route, route < 0 ? BreakablePropKind.Urn : BreakablePropKind.Crate);
                BreakableProp.Create(route < 0 ? 286f : 238f, route < 0 ? -3.8f : 3.8f,
                    route, BreakablePropKind.Portrait);
                MirrorScare.Create(route < 0 ? 276f : 241f, route < 0 ? -1 : 1, route);
            }
        }

        private void BuildForgottenDungeon()
        {
            CreateCastleGate(310f);
            for (float z = 313f; z < 333f; z += 7f)
            {
                CreateArch(z, greenGlow);
                CreateHangingChain(z + 2.5f, ((int)z % 2 == 0 ? -3.7f : 3.7f));
            }
            CreateCobweb(318f, -1);
            CreateCobweb(329f, 1);
            HiddenMonster.Create(321f, -1, 0, HiddenMonsterKind.Tomb);
            WallDoorScare.Create(327f, 1, 0, wood, darkStone, rail);
            CreateGraveyardProp("Models/KenneyGraveyard/gravestone-broken", 317f, 1, 0, 1.65f, -9f);
            CreateGraveyardProp("Models/KenneyGraveyard/shovel-dirt", 330f, -1, 0, 1.75f, 12f);
            CreateMist(315f, 0);
            CreateMist(328f, 0);
            CreateCeilingSpider(324f, -1, 0);
            StalkingMonster.Create(326f, -1, 0, 2);
            BreakableProp.Create(319f, 3.7f, 0, BreakablePropKind.Crate);
            BreakableProp.Create(331f, -3.7f, 0, BreakablePropKind.Urn);
        }

        private void BuildFinalHall()
        {
            CreateCastleGate(334f);
            for (float z = 337f; z < TrackLength; z += 6f)
            {
                CreateArch(z, amberGlow);
                CreateTorch(z + 2.2f, z % 12f < 1f ? -1 : 1);
            }
            CreateArmor(342f, -1);
            CreateArmor(342f, 1);
            // Slutporten ligger tillräckligt långt före stoppunkten för att vagnen
            // tydligt ska hinna se den öppnas och sedan köra helt igenom den.
            float endZ = TrackLength - 14f;
            float endX = TrackCenter(endZ);
            RouteDoor.Create(endZ, 0, wood, rail, amberGlow, true).transform.SetParent(root, true);
            CreateCobweb(352f, 1);
            CreateMist(340f, 0);
            CreateMist(365f, 0);
        }
    }
}