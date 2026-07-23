using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate int TimerDelegate();

public class Timer {

    private float delay;
    private short repeatTimes;
    private float residueTime;
    private short residueRepeatTimes;
    private TimerDelegate task;

    public LinkedListNode<Timer> ListNode;
    public bool valid = true;
    public int code;
    public bool unscaled;

    public Timer(float delay, short repeatTimes, TimerDelegate task, bool unscaled = false) {
        ResetTimer(delay, repeatTimes, task, unscaled);
    }

    /// <summary>
    /// 重置Timer的参数
    /// </summary>
    /// <param name="delay">延时</param>
    /// <param name="repeatTimes">重复次数</param>
    /// <param name="task">定时器回调</param>
    /// <param name="unscaled">是否使用Time.unscaledDeltaTime</param>
    public void ResetTimer(float delay, short repeatTimes, TimerDelegate task, bool unscaled = false) {
        valid = true;
        this.delay = delay;
        this.repeatTimes = repeatTimes;
        this.task = task;
        residueRepeatTimes = repeatTimes;
        residueTime = this.delay;
        this.unscaled = unscaled;
    }

    /// <summary>
    /// 清理Timer持有的委托和链表信息
    /// </summary>
    public void Clear() {
        task = null;
        ListNode = null;
    }

    /// <summary>
    /// 按帧执行刷新
    /// </summary>
    public bool Update() {
        if (!valid) {
            return false;
        }

        residueTime -= unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        if (residueTime > 0) {
            //定时间隔时间还未到
            return false;
        }

        var result = 0;
        try {
            result = task.Invoke();
        }
        catch (Exception e) {
            Debug.LogError("定时器执行失败：\n" + e);
        }

        if (result < 0) {
            //函数返回<0 标识需要立即终止定时器
            return true;
        }

        switch (repeatTimes) {
            case > 0: {
                residueRepeatTimes--;
                residueTime += delay; //减少时间上的精度损失
                if (residueRepeatTimes <= 0) {
                    return true;
                }

                break;
            }
            case -1:
                //_repeatTimes值为-1 说明是无限定时器
                residueTime += delay;
                break;
            default:
                return false;
        }

        return false;
    }
}