using UnityEngine;

// 在 Project 的 Create 菜单中提供配置资源创建入口
[CreateAssetMenu(
    fileName = "PlayerStats",
    menuName = "Demo/Player Stats")]
public class PlayerStats : EntityStats<PlayerStats>
{
    [Header("Moving Stats")]
    // 目标最高水平速度，单位为米每秒
    public float topSpeed = 4f;

    // 沿输入方向增加速度的快慢
    public float acceleration = 6f;

    // 松开输入后降低速度的快慢
    public float friction = 6f;

    // 转向时消除旧方向残余速度的快慢
    public float turningDrag = 12f;
    
    [Header("Running Stats")]

    public float runningTopSpeed = 6f;

    public float runningAcceleration = 8f;

    public float runningTurningDrag = 12f;
    
    [Header("Gravity Stats")]

    // 向下增加速度的快慢，单位为米每平方秒
    public float gravity = 20f;

    // 最大下落速度，填写正数
    public float gravityTopSpeed = 30f;

    // 贴地时的小幅向下速度，填写正数
    public float snapForce = 2f;
    
    [Header("Jump Stats")]

    // 起跳时的向上初速度，单位为米每秒
    public float jumpSpeed = 7f;
}