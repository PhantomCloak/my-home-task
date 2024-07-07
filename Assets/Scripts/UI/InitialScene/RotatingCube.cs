using UnityEngine;

public class RotatingCube : MonoBehaviour {
	[SerializeField]
	private Vector3 m_Direction = Vector3.up; 
	[SerializeField]
	public float m_Speed = 10;
	
	void Update() {
		transform.Rotate(m_Direction * m_Speed * Time.deltaTime);
	}
}
