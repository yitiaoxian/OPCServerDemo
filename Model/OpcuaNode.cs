using System;

namespace Model
{
    public class OpcuaNode
    {
        /// <summary>
        /// 节点路径(逐级拼接)
        /// </summary>
        public string NodePath { get; set; }
        /// <summary>
        /// 父节点路径(逐级拼接)
        /// </summary>
        public string ParentPath { get; set; }
        /// <summary>
        /// 节点编号（唯一性没有敲定）
        /// </summary>
        public int NodeId { get; set; }
        /// <summary>
        /// 节点名称
        /// </summary>
        public string NodeName { get; set; }
        /// <summary>
        /// 是否是端点
        /// </summary>
        public bool IsTerminal { get; set; }
        /// <summary>
        /// 节点类型
        /// </summary>
        public NodeType NodeType { get; set; }
    }
    public enum NodeType
    {
        /// <summary>
        /// 根节点
        /// </summary>
        Scada = 1,
        /// <summary>
        /// 通道
        /// </summary>
        Channel = 2,
        /// <summary>
        /// 设备
        /// </summary>
        Device = 3,
        /// <summary>
        /// 测点
        /// </summary>
        Measure =4
    }
}
