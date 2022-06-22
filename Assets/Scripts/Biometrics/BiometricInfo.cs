using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Biometrics
{
    public class BiometricInfo  //used to be internal class
    {
        public Vector3 PlayerPos;
        public Quaternion HeadsetRot;

        public Vector3 EyeMov;

        public Vector3 ControllerMov;
        public Quaternion ControllerRot;
        public int ButtonPress;

        public int getLength()
        {
            return 6;
        }
    }
}
