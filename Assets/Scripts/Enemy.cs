﻿using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using UnityEditor;

public class Enemy : MonoBehaviour
{
    public float idleSpeed = 1.0f;
    public Vector2 idleVector;
    private Coroutine idleCoroutine;
    public LayerMask playerLayer;
    public bool isIdle;
    AIPath aipath;
    AIDestinationSetter aIDestinationSetter;
    Animator gfxAnim;
    SpriteRenderer gfxSr;
    CircleCollider2D circleCollider2D;
    private Rigidbody2D rb;
    public float distToPlayer;
    public Transform player;


    [Header("Artwork:")]
    public EnemyScriptObj enemyScriptObj;
    [SerializeField]
    public GameObject projectile;


    [Header("Enemy Stats:")]
    public float maxHealth;
    public float currentHealth;
    public string enemyName;
    public int meleeDamage;
    public int rangedDamage;
    public float rangedAttackSpeed;
    public float attackDelayTime;
    public float attackRange;
    public float movementSpeed;
    public float colliderRadius;
    public bool enemyIsRanged;
    public float enemyViewRange;


    void Awake()
    {
        // Get a reference to physics component
        rb = GetComponent<Rigidbody2D>();
        // Get a reference to AIPATH component
        aipath = GetComponent<AIPath>();
        circleCollider2D = GetComponent<CircleCollider2D>();
    }

    void Start()
    {
        // initialize player transform
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();

        // init things
        initStats();
        initTarget();
        initArt();
        initUniqueScript();

        // idle
        idleCoroutine = StartCoroutine(Idle());
    }

    // gets unique script
    void initUniqueScript()
    {
        if (enemyScriptObj.uniqueScript)
        {
            var script = ObjectNames.GetDragAndDropTitle(enemyScriptObj.uniqueScript).Replace(" (MonoScript)", "");
            // print(script);
            Type scriptType = Type.GetType(script);

            // print(scriptType);
            gameObject.AddComponent(scriptType);
        }
    }

    void initStats()
    {
        var es = enemyScriptObj;
        maxHealth = es.maxHealth;
        currentHealth = maxHealth;

        // set gameobject name
        enemyName = es.enemyName;
        gameObject.name = enemyName;

        meleeDamage = es.meleeDamage;
        rangedDamage = es.rangedDamage;
        rangedAttackSpeed = es.rangedAttackSpeed;
        attackDelayTime = es.attackDelayTime;
        attackRange = es.attackRange;

        // set max speed in AIPath component
        movementSpeed = es.movementSpeed;
        aipath.maxSpeed = movementSpeed;

        // set circle collider radius
        colliderRadius = es.colliderRadius;
        circleCollider2D.radius = colliderRadius;

        projectile = es.projectile;
        enemyIsRanged = es.enemyIsRanged;
        enemyViewRange = es.enemyViewRange;

    }

    // initializes target
    void initTarget()
    {
        aIDestinationSetter = GetComponent<AIDestinationSetter>();
        aIDestinationSetter.target = player;
    }

    // initializes animator and sprite renderer
    void initArt()
    {
        // gets animator of enemyGFX
        gfxAnim = transform.GetChild(0).GetComponent<Animator>();

        // gets sprite renderer of enemyGFX
        gfxSr = transform.GetChild(0).GetComponent<SpriteRenderer>();
        gfxAnim.runtimeAnimatorController = enemyScriptObj.controller;
        gfxSr.sprite = enemyScriptObj.artwork;
    }

    private void Update()
    {
        flipEnemy();

        // gets distance to player
        distToPlayer = Vector2.Distance(player.transform.position, transform.position);

        manageState();

        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }

    }

    // manages enemy state
    void manageState()
    {

        // if distance from enemy to player is greater than the enemies' view range, enemy stops searching. 
        if (distToPlayer > enemyViewRange)
        {
            aipath.canSearch = false;

            // disable ai path component so idle coroutine can be played. (this allows for rigidbody to be manipulated)
            aipath.enabled = false;
        }
        else
        {
            // if enemy is a ranged character, the enemy will stop searching when enemy is within it's attack range.
            if (enemyIsRanged)
            {
                if (distToPlayer <= attackRange)
                {
                    aipath.canSearch = false;

                }
            }
            else
            {
                aipath.canSearch = true;
            }

            // enable ai path component so idle coroutine will be stopped
            // enemy will go back to following player
            aipath.enabled = true;
        }
    }

    // flips enemy. uses aipath module
    private void flipEnemy()
    {
        if (aipath.desiredVelocity.x >= 0.01f)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }
        else if (aipath.desiredVelocity.x <= -0.01f)
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
    }

    public void TakeDamage(float damage, float critPercent, float weaponKnockBack)
    {
        Color32 popUpColorNormal = new Color32(255, 255, 255, 255);
        Color32 popUpColorCrit = new Color32(209, 0, 53, 255);
        bool isCrit = false;

        // random number determines critical hit
        float rand = UnityEngine.Random.Range(0.0f, 1.0f);
        Vector2 enemyPos = transform.position;

        float dirY = UnityEngine.Random.Range(0.0f, 0.8f);
        float dirX = UnityEngine.Random.Range(-0.8f, 0.8f);

        // GetWeapon.weaponCriticalChance is a float from 0.0f to 1.0f. All numbers <= this number will be considered the 'critical' chance.
        if (rand == critPercent)
        {
            // hits 3 times! 1/100 chance
            for (var i = 0; i < 2; i++)
            {
                dirY = UnityEngine.Random.Range(0.0f, 0.8f);
                dirX = UnityEngine.Random.Range(-1.0f, 1.0f);
                enemyPos = new Vector2(enemyPos.x + dirX, enemyPos.y + dirY);
                isCrit = true;

                Popup.Create(enemyPos, damage, popUpColorCrit, isCrit);
            }
        }
        else if (rand < critPercent)
        {
            // one critical strike. hits 1 time and has a chance depending on the weapon
            damage *= 2;
            popUpColorNormal = popUpColorCrit;
            isCrit = true;
        }

        enemyPos = new Vector2(enemyPos.x + dirX, enemyPos.y + dirY);

        // creates damage pop up
        Popup.Create(enemyPos, damage, popUpColorNormal, isCrit);

        currentHealth -= damage;
        // chakes camera
        FindObjectOfType<CameraShake>().Shake(0.01f, 0.01f);

        // starts hurt and knockback coroutines
        StartCoroutine(Hurt());
        StartCoroutine(Knockback(weaponKnockBack));
    }

    IEnumerator Knockback(float weaponKnockBack)
    {
        // Vector2 difference = (transform.position - player.transform.position) * weaponKnockBack;

        // yield return new WaitForSeconds(0.2f);
        // transform.position = new Vector2(transform.position.x + difference.x, transform.position.y + difference.y);
        aipath.enabled = false;
        // Vector2 difference = (transform.position - player.transform.position).normalized * weaponKnockBack;
        // rb.AddForce(difference, ForceMode2D.Impulse);

        // add stunned animation?
        // print("stun");

        yield return new WaitForSeconds(weaponKnockBack);
        aipath.enabled = true;
    }

    IEnumerator Hurt()
    {
        // plays hurt animation
        gfxAnim.Play("hurt");
        yield return new WaitForSeconds(0.1f);
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            FindObjectOfType<HealthBarManager>().getHearts("damage", meleeDamage);
        }
        else if (other.gameObject.tag == "Obstacle")
        {
            transform.position = transform.position;
        }
    }

    IEnumerator Shoot()
    {
        while (true)
        {
            if (distToPlayer <= attackRange)
            {
                Vector3 enemyPos = transform.position;
                Vector3 playerPos = player.transform.position;

                // GameObject projectileInstance = Instantiate(es.projectile, enemyPos, transform.rotation);
                GameObject projectileInstance = ObjectPooler.i.SpawnFromPool(projectile.name, enemyPos, transform.rotation);
                // print("brown " + projectile.name);

                // set damage in projectile script
                projectileInstance.GetComponent<Projectile>().projectileDamage = rangedDamage;

                Rigidbody2D projRB = projectileInstance.GetComponent<Rigidbody2D>();


                // find vector between player position and enemy position
                Vector3 vector = (playerPos - enemyPos).normalized * (rangedAttackSpeed * 5);

                // debug line
                Debug.DrawLine(enemyPos, vector + enemyPos, Color.red, attackDelayTime);

                // print(vector);
                projRB.AddForce(vector);
            }
            // waits for attackDelayTime seconds
            yield return new WaitForSeconds(attackDelayTime);
        }
    }

    IEnumerator Idle()
    {
        while (true)
        {
            float mag = 0.8f;
            // Move in a direction
            float dirX = UnityEngine.Random.Range(-mag, mag);
            float dirY = UnityEngine.Random.Range(-mag, mag);

            idleVector = new Vector2(idleSpeed * dirX, idleSpeed * dirY);
            // print(dirX.ToString() + "    " + dirY.ToString());
            rb.velocity = idleVector;

            if (rb.velocity.x >= 0.01f)
            {
                transform.localScale = new Vector3(-1f, 1f, 1f);
            }
            else if (rb.velocity.x <= -0.01f)
            {
                transform.localScale = new Vector3(1f, 1f, 1f);
            }

            float wait = UnityEngine.Random.Range(0.7f, 4f);
            // Wait
            yield return new WaitForSeconds(wait);

        }
    }

    // draws attackRange from transform.position
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, enemyViewRange);
    }

}
