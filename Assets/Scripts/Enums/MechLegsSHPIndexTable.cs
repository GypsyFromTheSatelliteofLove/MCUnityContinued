/// <summary>
/// This is the orig incdices for the SHP files in game
/// the legs are offset from the rest of the parts because of duplicate legs
/// to get appropriate legs we can use:
/// if (mechIndex == MechSHPIndexTable.HUNCHBACK_IIC) currentPartIndex = 1; // if mechindex == 16, legsindex = 1
/// else if (mechIndex == MechSHPIndexTable.FIRESTARTER) currentPartIndex = 7; // if mechindex == 11, legsindex = 7
/// else if (mechIndex >= MechSHPIndexTable.VULTURE) currentPartIndex -= 5;// if mechindex >= 15, legsindex = mechindex - 5
/// else if (mechIndex >= MechSHPIndexTable.MASAKARI) currentPartIndex -= 3; // if mechindex >= 12, legsindex = mechindex - 3
/// else if (mechIndex >= MechSHPIndexTable.ULLER) currentPartIndex -= 2;
/// </summary>
public enum MechsLegsSHPIndexTable
{
    AWESOME = 0,
    HUNCHB_HUNCHB_IIC = 1,
    CATAPULT = 2,
    JAEGERMECH = 3,
    COUGAR_ULLER = 4,
    THOR_LOKI = 5,
    ATLAS = 6,
    COMMANDO_FIRESTR = 7,
    CENTURION = 8,
    MASAKARI = 9,
    MAD_CAT_VULT = 10,
    RAVEN = 11,
    HOLLANDER = 12,
    STILLETO = 13,
    SHADOW_CAT = 14,
    NOVA_CAT = 15,
    TURKINA = 16,
    MAULER = 17,
    BUSHWACKER = 18
}