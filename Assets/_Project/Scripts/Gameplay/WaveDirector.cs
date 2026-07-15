using System.Collections.Generic;
using Hollowwest.Navigation;
using Hollowwest.Prototype;
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
    private Material _eyeMaterial;
    private bool _running;
    private int _remainingToSpawn;
    private float _spawnTimer;
    private int _spawnIndex;
    private float _spawnX = 46f;
    private float _spawnZ = 32f;

    public bool IsNightComplete => _running && _remainingToSpawn <= 0 && EnemyUnit.ActiveEnemies.Count == 0;

    public void Initialize(
        GridNavigationService navigation,
        CampCore campCore,
        Material enemyMaterial,
        Bounds worldBounds)
    {
        _navigation = navigation;
        _campCore = campCore;
        _enemyMaterial = enemyMaterial;
        _eyeMaterial = new Material(Shader.Find("Standard"))
        {
            color = new Color(1f, 0.24f, 0.12f),
            hideFlags = HideFlags.DontSave
        };
        _eyeMaterial.EnableKeyword("_EMISSION");
        _eyeMaterial.SetColor("_EmissionColor", new Color(1f, 0.08f, 0.02f));
        _spawnX = Mathf.Max(8f, worldBounds.extents.x - 4f);
        _spawnZ = Mathf.Max(6f, worldBounds.extents.z - 4f);
    }

    public void StartNight(int nightNumber)
    {
        _running = true;
        _remainingToSpawn = enemiesPerNight + Mathf.Max(0, nightNumber - 1) * 4;
        _spawnTimer = 0f;
        _spawnIndex = 0;
    }

    public void StopNight(bool destroyEnemies)
    {
        _running = false;
        _remainingToSpawn = 0;
        _spawnTimer = 0f;

        if (!destroyEnemies)
        {
            return;
        }

        List<EnemyUnit> enemies = new(EnemyUnit.ActiveEnemies);
        foreach (EnemyUnit enemy in enemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
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
            new Vector3(_spawnX, 0f, _spawnZ),
            new Vector3(_spawnX, 0f, -_spawnZ),
            new Vector3(-_spawnX, 0f, _spawnZ),
            new Vector3(-_spawnX, 0f, -_spawnZ)
        };

        Vector3 position = spawnPoints[_spawnIndex % spawnPoints.Length];
        _spawnIndex++;

        GameObject enemy = new("Night Fiend");
        enemy.name = "Night Fiend";
        enemy.transform.SetParent(transform);
        enemy.transform.position = position;

        CapsuleCollider bodyCollider = enemy.AddComponent<CapsuleCollider>();
        bodyCollider.center = new Vector3(0f, 0.82f, 0f);
        bodyCollider.radius = 0.46f;
        bodyCollider.height = 1.65f;

        StylizedCharacterBuilder.BuildEnemy(
            enemy.transform,
            new Material(_enemyMaterial),
            _eyeMaterial);

        NavigationAgent agent = enemy.AddComponent<NavigationAgent>();
        agent.Initialize(_navigation);
        agent.Speed = 2.5f;

        EnemyUnit enemyUnit = enemy.AddComponent<EnemyUnit>();
        enemyUnit.Initialize(agent, _campCore);
    }
}
}
