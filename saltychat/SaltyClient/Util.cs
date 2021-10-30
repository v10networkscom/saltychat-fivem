using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using SaltyShared;

namespace SaltyClient
{
    public static class Util
    {
        #region Player Extensions
        public static string GetTeamSpeakName(this Player player) => player.State[State.SaltyChat_TeamSpeakName];
        public static float GetVoiceRange(this Player player) => player.State[State.SaltyChat_VoiceRange] ?? 0f;
        public static bool GetIsAlive(this Player player) => player.State[State.SaltyChat_IsAlive] == true;
        #endregion

        #region Vehicle Extensions
        public static bool HasOpening(this Vehicle vehicle)
        {
            VehicleDoor[] doors = vehicle.Doors.GetAll();

            return doors.Length == 0 || doors.Any(d => d.Index != VehicleDoorIndex.Hood && (d.IsBroken || d.IsOpen)) ||
                    !vehicle.Windows.AreAllWindowsIntact || vehicle.Windows.GetAllWindows().Any(w => !w.IsIntact) || // AreAllWindowsIntact = damage on all windows (also bullet holes) | IsIntact = also true if window is rolled down
                    (vehicle.IsConvertible && vehicle.RoofState != VehicleRoofState.Closed);
        }
        #endregion
    }
}
