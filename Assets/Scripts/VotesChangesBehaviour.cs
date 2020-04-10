using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VotesChangesBehaviour : MonoBehaviour
{
    private TextMeshPro textMesh;
    private Rigidbody2D myRigidbody;

    internal void Show(int votes)
    {
        textMesh = GetComponent<TextMeshPro>();
        myRigidbody = GetComponent<Rigidbody2D>();

        var votesStr = votes.ToString().PadLeft(2, '+');
        votesStr = string.Format("{0}<size=40%>,000</size>", votesStr);
        textMesh.text = votesStr;

        var color = votes > 0 ? Color.white : Color.red;
        textMesh.color = color;

        myRigidbody.velocity = new Vector3(0, 1, 0);
    }

    void Update()
    {
        var color = textMesh.color;
        var newColor = new Color(color.r, color.g, color.b, color.a - 0.01f);
        textMesh.color = newColor;

        if (newColor.a <= 0) Destroy(gameObject);
    }
}
