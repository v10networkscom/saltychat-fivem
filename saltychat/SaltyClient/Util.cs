using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;

namespace SaltyClient
{
    public static class Util
    {
        public static string ToJson(object obj)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }

        public static bool TryParseJson<T>(string json, out T result)
        {
            result = default;

            try
            {
                result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            }
            catch { }

            return result is object;
        }

        public static bool HasOpening(this Vehicle vehicle)
        {
            VehicleDoor[] doors = vehicle.Doors.GetAll();

            return doors.Length == 0 || doors.Any(d => d.Index != VehicleDoorIndex.Hood && (d.IsBroken || d.IsOpen)) ||
                    !vehicle.Windows.AreAllWindowsIntact || vehicle.Windows.GetAllWindows().Any(w => !w.IsIntact) || // AreAllWindowsIntact = damage on all windows (also bullet holes) | IsIntact = also true if window is rolled down
                    (vehicle.IsConvertible && vehicle.RoofState != VehicleRoofState.Closed);
        }
    }
}
