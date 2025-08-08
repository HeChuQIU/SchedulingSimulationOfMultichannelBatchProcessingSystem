using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingSimulationOfMultichannelBatchProcessingSystem.Models
{
    /// <summary>
    /// 表示作业的数据结构
    /// </summary>
    public class Job(string jobName, uint arrivalTime, uint serviceTime, uint memoryUse, uint tapeUnitUse)
    {
        /// <summary>
        /// 作业名称
        /// </summary>
        public string JobName { get; set; } = jobName;

        /// <summary>
        /// 到达时间
        /// </summary>
        public uint ArrivalTime { get; set; } = arrivalTime;

        /// <summary>
        /// 服务时间
        /// </summary>
        public uint ServiceTime { get; set; } = serviceTime;

        /// <summary>
        /// 累计运行时间
        /// </summary>
        public uint RunningTime { get; set; } = 0;

        /// <summary>
        /// 剩余服务时间
        /// </summary>
        public uint RemainingServiceTime => ServiceTime - RunningTime;

        /// <summary>
        /// 内存占用
        /// </summary>
        public uint MemoryUse { get; set; } = memoryUse;

        /// <summary>
        /// 磁带机占用
        /// </summary>
        public uint TapeUnitUse { get; set; } = tapeUnitUse;

        /// <summary>
        /// 已运行时间
        /// </summary>
        public uint ProcessedTime { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public uint FinishTime { get; set; }

        /// <summary>
        /// 周转时间
        /// </summary>
        public uint TurnaroundTime => FinishTime - ArrivalTime;

        /// <summary>
        /// 带权周转时间
        /// </summary>
        public double WeightedTurnaroundTime => (double)TurnaroundTime / ServiceTime;

        public void Reset()
        {
            RunningTime = 0;
            ProcessedTime = 0;
            FinishTime = 0;
        }
    }
}