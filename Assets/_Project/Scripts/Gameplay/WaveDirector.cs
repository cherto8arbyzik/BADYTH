using Hollowwest.Navigation;
using UnityEngine;

namespace Hollowwest.Gameplay
{

public sealed class WaveDirector : MonoBehaviour
{
    [SerializeField] private int enemiesPerNight = 10;
    [SerializeField] private float spawnInterval = 1.2f;

    private GridNavigationService _navigation;
    private CampCore _campCore;
    private Material _enemyMaterial;
    private bool _running;
    private int _remainingToSpawn;
    private float _spawnTimer;
    private int _spawnIndex;

    public bool IsNightComplete => _running && _remainingToSpawn <= 0 && EnemyUnit.ActiveEnemies.Count == 0;

    public void Initialize(GridNavigationService navigation, CampCore campCore, Material enemyMaterial)
    {
        _navigation = navigation;
        _campCore = campCore;
        _enemyMaterial = enemyMaterial;
    }

    public void StartNight(int nightNumber)
    {
        _running = true;
        _remainingToSpawn = enemiesPerNight + Mathf.Max(0, nightNumber - 1) * 4;
        _spawnTimer = 0f;
        _spawnIndex = 0;
    }

    private void Update()
    {
        if (!_running || _remainingToSpawn <= 0)
        {
            return;
        }

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f)
        {
            return;
        }

        _spawnTimer = spawnInterval;
        _remainingToSpawn--;
        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        Vector3[] spawnPoints =
        {
            new Vector3(10.5f, 0.65f, 7.5f),
            new Vector3(10.5f, 0.65f, -7.5f),
            new Vector3(-10.5f, 0.65f, 7.5f),
            new Vector3(-10.5f, 0.65f, -7.5f)
        };

        Vector3 position = spawnPoints[_spawnIndex % spawnPoints.Length];
        _spawnIndex++;

        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = "Night Fiend";
        enemy.transform.SetParent(transform);
        enemy.transform.position = position;
        enemy.transform.localScale = new Vector3(0.68f, 0.72f, 0.68f);
        enemy.GetComponent<Renderer>().sharedMaterial = new Material(_enemyMaterial);

        NavigationAgent agent = enemy.AddComponent<NavigationAgent>();
        agent.Initialize(_navigation);
        agent.Speed = 2.5f;

        EnemyUnit enemyUnit = enemy.AddComponent<EnemyUnit>();
        enemyUnit.Initialize(agent, _campCore);
    }
}
}
