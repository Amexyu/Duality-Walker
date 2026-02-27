using UnityEngine;

public sealed class Bomb : MonoBehaviour
{
    // 爆弾に Collider（2Dなら Collider2D）と、適切な Layer/Tag を設定してください。
    // プレイヤー側にも Collider と Rigidbody が必要です（2Dなら Rigidbody2D）。
    // トリガーにする場合は「Is Trigger」をオン。

    // 2Dの場合はこちらを使用
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 例: プレイヤーのタグが "Player" の場合のみ反応
        if (other.CompareTag("Player"))
        {
            // 爆弾を非表示にする／消す
            // 完全に消すなら Destroy、見えなくするだけなら SetActive(false)
            Destroy(gameObject);
            // gameObject.SetActive(false); // 見えなくするだけの場合
        }
    }

    // 3Dの場合はこちらを使用
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Destroy(gameObject);
            // gameObject.SetActive(false);
        }
    }
}