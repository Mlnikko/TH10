using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitEffect : MonoBehaviour
{
    // 帧动画图片数组（在Inspector中赋值）
    public Sprite[] effectFrames = new Sprite[4];

    // 每帧持续时间（秒）
    public float frameDuration = 0.1f;

    // 旋转速度（度/秒）
    public float rotationSpeed = 180f;

    SpriteRenderer spriteRenderer;
    float timer;
    int currentFrame;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (effectFrames.Length == 0)
        {
            Debug.LogError("No effect frames assigned!");
            return;
        }

        spriteRenderer.sprite = effectFrames[0];
        currentFrame = 0;
        timer = 0f;
    }

    void Update()
    {
        if (effectFrames.Length == 0) return;

        // 旋转效果
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);

        // 帧动画
        timer += Time.deltaTime;

        if (timer >= frameDuration)
        {
            timer = 0f;
            currentFrame++;

            // 播放所有帧后销毁对象
            if (currentFrame >= effectFrames.Length)
            {
                Destroy(gameObject);
                return;
            }

            // 更新精灵显示
            spriteRenderer.sprite = effectFrames[currentFrame];
        }
    }
}
