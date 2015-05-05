using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiWindows
{
    public class MiState
    {
        public DateTime ReportTimeStamp;
        public bool A, B, X, Y;
        public bool DpadUp, DpadDown, DpadLeft, DpadRight;
        public bool L1, LS, R1, RS;
        public bool Back, Menu, HomeSimulated;
        //, Touch1, Touch2, TouchButton, TouchRight, TouchLeft;
        //public byte Touch1Identifier, Touch2Identifier;
        public byte LX, RX, LY, RY, LT, RT;
        //public byte FrameCounter; // 0, 1, 2...62, 63, 0....
        //public byte TouchPacketCounter; // we break these out automatically
        public byte Battery; // 0 for charging, 10/20/30/40/50/60/70/80/90/100 for percentage of full

        public MiState()
        {
            A = B = X = Y = false;
            DpadUp = DpadDown = DpadLeft = DpadRight = false;
            L1 = LS = R1 = RS = false;
            Back = Menu = HomeSimulated = false;
                //Touch1 = Touch2 = TouchButton =  TouchRight = TouchLeft = false;
            LX = RX = LY = RY = 127;
            LT = RT = 0;
            //FrameCounter = 255; // only actually has 6 bits, so this is a null indicator
            //TouchPacketCounter = 255; // 8 bits, no great junk value
            Battery = 0;
        }

        public MiState(MiState state)
        {
            ReportTimeStamp = state.ReportTimeStamp;
            A = state.A;
            B = state.B;
            X = state.X;
            Y = state.Y;
            DpadUp = state.DpadUp;
            DpadDown = state.DpadDown;
            DpadLeft = state.DpadLeft;
            DpadRight = state.DpadRight;
            L1 = state.L1;
            LS = state.LS;
            LT = state.LT;
            R1 = state.R1;
            RS = state.RS;
            RT = state.RT;
            Back = state.Back;
            Menu = state.Menu;
            HomeSimulated = state.HomeSimulated;
            //Touch1 = state.Touch1;
            //TouchRight = state.TouchRight;
            //TouchLeft = state.TouchLeft;
            //Touch1Identifier = state.Touch1Identifier;
            //Touch2 = state.Touch2;
            //Touch2Identifier = state.Touch2Identifier;
            //TouchButton = state.TouchButton;
            //TouchPacketCounter = state.TouchPacketCounter;
            LX = state.LX;
            RX = state.RX;
            LY = state.LY;
            RY = state.RY;
            //FrameCounter = state.FrameCounter;
            Battery = state.Battery;
        }

        public MiState Clone()
        {
            return new MiState(this);
        }

        public void CopyTo(MiState state)
        {
            state.ReportTimeStamp = ReportTimeStamp;
            state.A = A;
            state.B = B;
            state.X = X;
            state.Y = Y;
            state.DpadUp = DpadUp;
            state.DpadDown = DpadDown;
            state.DpadLeft = DpadLeft;
            state.DpadRight = DpadRight;
            state.L1 = L1;
            state.LS = LS;
            state.LT = LT;
            state.R1 = R1;
            state.RS = RS;
            state.RT = RT;
            state.Back = Back;
            state.Menu = Menu;
            state.HomeSimulated = HomeSimulated;
            //state.Touch1 = Touch1;
            //state.Touch1Identifier = Touch1Identifier;
            //state.Touch2 = Touch2;
            //state.Touch2Identifier = Touch2Identifier;
            //state.TouchLeft = TouchLeft;
            //state.TouchRight = TouchRight;
            //state.TouchButton = TouchButton;
            //state.TouchPacketCounter = TouchPacketCounter;
            state.LX = LX;
            state.RX = RX;
            state.LY = LY;
            state.RY = RY;
            //state.FrameCounter = FrameCounter;
            state.Battery = Battery;
        }

    }
}
