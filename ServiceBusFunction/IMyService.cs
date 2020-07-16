using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceBusFunction
{
  public  interface IMyService
    {
        string GetKey();
    }
    public class myClass : IMyService
    {
     public   string GetKey()
        {
            return "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";
        }
    }
}
