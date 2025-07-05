using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaperPilot.Model
{
    public enum PaperState
    {
        NotAnalyzed = -1,
        Keep = 0,
        Empty = 1,
        SplittingPoint = 2,
    }
    public class Paper
    {
        public PaperState State { get; set; } = PaperState.NotAnalyzed;
        public int PageId { get; set; } = -1;
        public ImageTexture? PagePreview { get; set; } = null;
    }
}
