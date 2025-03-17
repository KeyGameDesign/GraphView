using System.Collections;
using System.Collections.Generic;
public partial class Test1 : GraphViewRuntimeNodeBase
{
    public Test1() : base()
    {
        m_Name = GetType().Name;
    }
    public Test1Data m_RuntimeData;
    public override void InitRuntimeData()
    {
        m_RuntimeData = (Test1Data)m_Data;
    }
}