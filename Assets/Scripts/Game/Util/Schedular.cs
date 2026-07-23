using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scheduler : Singleton<Scheduler>{
    /// <summary>
    /// 记录Timer的索引
    /// </summary>
    private static int timerCode;

    /// <summary>
    /// 当前启动的定时器
    /// </summary>
    private LinkedList<Timer> timers = new();

    // /// <summary>
    // /// 延迟帧启动的定时器
    // /// </summary>
    // private LinkedList<FrameTimer> frameTimers = new LinkedList<FrameTimer>();

    /// <summary>
    /// Timer 查找表
    /// </summary>
    private Dictionary<int, Timer> lookUp = new();

    /// <summary>
    /// 定时指定时间触发回调
    /// </summary>
    /// <param name="delay">触发回调延时</param>
    /// <returns>Timer对应的HashCode</returns>
    public int Delay(float delay, TimerDelegate task){
        var timer = new Timer(delay, 1, task);
        timer.ListNode = timers.AddLast(timer);
        return addTimerLook(timerCode++, timer);
    }

    /// <summary>
    /// 定时指定时间触发回调,使用Time.unscaledDeltaTime不受timeScale影响
    /// </summary>
    public int DelayUnscaled(float delay, TimerDelegate task){
        var timer = new Timer(delay, 1, task, true);
        timer.ListNode = timers.AddLast(timer);
        return addTimerLook(timerCode++, timer);
    }


    /// <summary>
    /// 循环定时器 （一直循环）
    /// </summary>
    /// <returns>Timer对应的HashCode</returns>
    public int RepeatForever(float delay, TimerDelegate task){
        var timer = new Timer(delay, -1, task);
        timer.ListNode = timers.AddLast(timer);
        return addTimerLook(timerCode++, timer);
    }

    /// <summary>
    /// 循环定时器 （一直循环）,使用Time.unscaledDeltaTime不受timeScale影响
    /// </summary>
    /// <returns>Timer对应的HashCode</returns>
    public int RepeatForeverUnscaled(float delay, TimerDelegate task){
        var timer = new Timer(delay, -1, task, true);
        timer.ListNode = timers.AddLast(timer);
        return addTimerLook(timerCode++, timer);
    }

    /// <summary>
    /// 循环定时器 定时器指定循环次数
    /// </summary>
    /// <returns>Timer对应的HashCode</returns>
    public int Repeat(float delay, short repeatTimes, TimerDelegate task){
        var timer = new Timer(delay, repeatTimes, task);
        timer.ListNode = timers.AddLast(timer);
        return addTimerLook(timerCode++, timer);
    }

    /// <summary>
    /// 循环定时器 定时器指定循环次数,使用Time.unscaledDeltaTime不受timeScale影响
    /// </summary>
    /// <returns>Timer对应的HashCode</returns>
    public int RepeatUnscaled(float delay, short repeatTimes, TimerDelegate task){
        var timer = new Timer(delay, repeatTimes, task, true);
        timer.ListNode = timers.AddLast(timer);
        return addTimerLook(timerCode++, timer);
    }

    public void Update(){
        if (timers is not{ Count: > 0 })
            return;
        var now = timers.First;
        while (null != now){
            var next = now.Next;
            var value = now.Value;
            if (value.Update()){
                StopTimer(now.Value.code);
            }

            now = next;
        }
    }

    public void PauseTimer(int code){
        if (lookUp.TryGetValue(code, out var timer)){
            timer.valid = false;
        }
    }

    public void ResumeTimer(int code){
        if (lookUp.TryGetValue(code, out var timer)){
            timer.valid = true;
        }
    }

    public void StopTimer(int code){
        if (lookUp.TryGetValue(code, out var timer)){
            if (timer.ListNode != null){
                timers.Remove(timer.ListNode);
                timer.ListNode = null;
                removeTimerLook(code);
            }
        }
    }

    private int addTimerLook(int key, Timer timer){
        if (lookUp.ContainsKey(key)){
            Debug.LogError("重复添加Timer");
            return 0;
        }

        lookUp.Add(key, timer);
        timer.code = key;
        return key;
    }

    private void removeTimerLook(int key){
        if (lookUp.ContainsKey(key)){
            var timer = lookUp[key];
            timer.Clear();
            lookUp.Remove(key);
        } else{
            Debug.LogError("移除不存在的定时器");
        }
    }

    private void OnDestroy(){
        timers.Clear();
        timerCode = 0;
    }
}