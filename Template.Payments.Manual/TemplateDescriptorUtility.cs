using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Template.Payments.Manual;

public class TemplateDescriptorUtility
{
    public string SystemName { get; }
    public string FriendlyName { get; }
    public string SimpleName { get; }
    public string AddressName { get; }

    public TemplateDescriptorUtility(string systemName, string friendlyName)
    {
        SystemName = systemName;
        FriendlyName = friendlyName;
        SimpleName = systemName.Split('.')[1];
        AddressName = systemName.Split('.')[0] + systemName.Split('.')[1];
    }
}
