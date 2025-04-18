using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurveyFunctions.Options;

public class AppSettings
{
    internal bool IsEncrypted { get; set; }

    public VoxcoApiOptions VoxcoApiOptions { get; set; }
}
