using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Gametest
{
    public partial class Form1 : Form
    {
        // =========================
        // РАЗМЕР МИРА
        // =========================

        const int WorldWidth = 3000;
        const int WorldHeight = 2000;

        // =========================
        // ИГРОК
        // =========================

        float playerX = 1450;
        float playerY = 950;

        const int PlayerWidth = 80;
        const int PlayerHeight = 80;
        // =========================
        // ГЛАВНОЕ МЕНЮ
        // =========================

        bool mainMenu = true;
        const float Speed = 5f;
        int playerHealth = 100;
        bool playerDead = false;
        int enemyAttackCooldown = 0;

        const int EnemyDamage = 20;
        const int EnemyAttackCooldownTime = 45;
        const int MaxPlayerHealth = 100;

        bool up;
        bool down;
        bool left;
        bool right;

        // =========================
        // КАМЕРА
        // =========================

        float cameraX;
        float cameraY;

        // =========================
        // КАРТИНКИ ИГРОКА
        // =========================

        Image playerFront;
        Image playerBack;
        Image playerLeft;
        Image playerRight;

        Image currentPlayerImage;

        // =========================
        // NPC
        // =========================

        float npcX = 1600;
        float npcY = 850;

        const int NpcWidth = 140;
        const int NpcHeight = 170;

        Image npcImage;

        // =========================
        // ВРАГ
        // =========================

        float enemyX = 1890;
        float enemyY = 470;

        const int EnemyWidth = 70;
        const int EnemyHeight = 70;

        Image enemyImage;

        int enemyHealth = 50;
        const int MaxEnemyHealth = 50;

        bool enemyAlive = true;

        // Состояние боя
        bool fightStarted = false;

        // Диалог врага
        bool enemyDialogueOpen = false;
        int enemyDialogueStep = 0;

        // =========================
        // ДИАЛОГ
        // =========================

        bool dialogueOpen = false;

        // =========================
        // КВЕСТ
        // =========================

        bool questStarted = false;
        bool questCompleted = false;

        int eggsCollected = 0;

        const int RequiredEggs = 5;

        // =========================
        // ЯЙЦА
        // =========================

        class Egg
        {
            public float X;
            public float Y;
            public bool Collected;
            public bool Guarded;

            public Egg(float x, float y, bool guarded = false)
            {
                X = x;
                Y = y;
                Collected = false;
                Guarded = guarded;
            }
        }

        List<Egg> eggs = new List<Egg>();

        const int EggWidth = 32;
        const int EggHeight = 42;

        // =========================
        // ОБЪЕКТЫ КАРТЫ
        // =========================

        List<Rectangle> obstacles = new List<Rectangle>();

        // =========================
        // ТАЙМЕР
        // =========================

        System.Windows.Forms.Timer gameTimer = new System.Windows.Forms.Timer();

        // =========================
        // CONSTRUCTOR
        // =========================

        public Form1()
        {
            InitializeComponent();

            DoubleBuffered = true;
            KeyPreview = true;

            Text = "Кирюха: В поисках яиц";

            ClientSize = new Size(1100, 700);

            LoadPlayerImages();

            string folder = AppDomain.CurrentDomain.BaseDirectory;

            npcImage = Image.FromFile(
                Path.Combine(folder, "npc.png")
            );
            enemyImage = Image.FromFile(
            Path.Combine(folder, "fight1.png")
            );

            CreateWorld();
            CreateEggs();

            currentPlayerImage = playerFront;

            gameTimer.Interval = 16;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();

            KeyDown += Form1_KeyDown;
            KeyUp += Form1_KeyUp;
        }

        // =========================
        // ЗАГРУЗКА КАРТИНОК
        // =========================

        void LoadPlayerImages()
        {
            string folder = AppDomain.CurrentDomain.BaseDirectory;

            playerFront = LoadImage(folder, "player_front.png");
            playerBack = LoadImage(folder, "player_back.png");
            playerLeft = LoadImage(folder, "player_right.png");
            playerRight = LoadImage(folder, "player_left.png");

            currentPlayerImage = playerFront;
        }

        Image LoadImage(string folder, string fileName)
        {
            string path = Path.Combine(folder, fileName);

            if (File.Exists(path))
            {
                return Image.FromFile(path);
            }

            // Если картинка не найдена,
            // создаём временную заглушку.
            Bitmap image = new Bitmap(80, 80);

            using (Graphics g = Graphics.FromImage(image))
            {
                g.Clear(Color.Transparent);

                using (Brush brush = new SolidBrush(Color.Red))
                {
                    g.FillEllipse(brush, 10, 10, 60, 60);
                }
            }

            return image;
        }

        // =========================
        // СОЗДАНИЕ МИРА
        // =========================

        void CreateWorld()
        {
            obstacles.Clear();

            // Верхняя часть
            obstacles.Add(new Rectangle(500, 300, 250, 80));
            obstacles.Add(new Rectangle(1000, 250, 300, 100));

            // Левая часть
            obstacles.Add(new Rectangle(300, 700, 120, 300));
            obstacles.Add(new Rectangle(600, 1100, 300, 100));

            // Правая часть
            obstacles.Add(new Rectangle(2000, 500, 300, 100));
            obstacles.Add(new Rectangle(2350, 900, 150, 300));

            // Нижняя часть
            obstacles.Add(new Rectangle(1200, 1500, 400, 100));
            obstacles.Add(new Rectangle(1900, 1400, 300, 120));
        }

        // =========================
        // СОЗДАНИЕ ЯИЦ
        // =========================

        void CreateEggs()
        {
            eggs.Clear();

            // Яйцо №1
            eggs.Add(new Egg(1100, 600));

            // Яйцо №2 — охраняется врагом
            eggs.Add(new Egg(1800, 450, true));

            // Яйцо №3
            eggs.Add(new Egg(2200, 750));

            // Яйцо №4
            eggs.Add(new Egg(900, 1350));

            // Яйцо №5
            eggs.Add(new Egg(2500, 1300));
        }

        // =========================
        // ОСНОВНОЙ ЦИКЛ
        // =========================

        void GameLoop(object sender, EventArgs e)
        {
            if (mainMenu)
            {
                Invalidate();
                return;
            }

            if (!dialogueOpen)
            {
                MovePlayer();
            }
            
            UpdateEnemyAttack();
            UpdateCamera();

            Invalidate();
        }
        // =========================
        // АТАКА ВРАГА
        // =========================

        void UpdateEnemyAttack()
        {
            if (!fightStarted)
                return;

            if (!enemyAlive)
                return;

            if (playerDead)
                return;

            if (!IsNearEnemy())
                return;

            if (enemyAttackCooldown > 0)
            {
                enemyAttackCooldown--;
                return;
            }

            playerHealth -= EnemyDamage;

            enemyAttackCooldown = EnemyAttackCooldownTime;

            if (playerHealth <= 0)
            {
                playerHealth = 0;

                PlayerDeath();
            }
        }
        // =========================
        // СМЕРТЬ ИГРОКА
        // =========================

        void PlayerDeath()
        {
            playerDead = true;

            up = false;
            down = false;
            left = false;
            right = false;

            fightStarted = false;

            Invalidate();
        }

        void UpdateEnemy()
        {
            float dx = playerX - enemyX;
            float dy = playerY - enemyY;

            float distance =
                MathF.Sqrt(dx * dx + dy * dy);

            // =========================
            // ВРАГ ИДЁТ К ИГРОКУ
            // =========================

            if (distance > 75)
            {
                float enemySpeed = 2.0f;

                enemyX += (dx / distance) * enemySpeed;
                enemyY += (dy / distance) * enemySpeed;
            }
            else
            {
                // =========================
                // ВРАГ АТАКУЕТ
                // =========================

                // Пока просто наносим урон
                // раз в несколько кадров
                playerHealth -= 1;

                if (playerHealth < 0)
                    playerHealth = 0;
            }
        }

        // =========================
        // ДВИЖЕНИЕ ИГРОКА
        // =========================

        void MovePlayer()
        {
            float newX = playerX;
            float newY = playerY;

            if (up)
            {
                newY -= Speed;
            }

            if (down)
            {
                newY += Speed;
            }

            if (left)
            {
                newX -= Speed;
            }

            if (right)
            {
                newX += Speed;
            }

            Rectangle newPlayerRect =
                new Rectangle(
                    (int)newX,
                    (int)newY,
                    PlayerWidth,
                    PlayerHeight
                );

            // Проверка столкновения
            bool collision = false;

            foreach (Rectangle obstacle in obstacles)
            {
                if (newPlayerRect.IntersectsWith(obstacle))
                {
                    collision = true;
                    break;
                }
            }

            if (!collision)
            {
                playerX = newX;
                playerY = newY;
            }

            // Ограничение по миру
            if (playerX < 0)
                playerX = 0;

            if (playerY < 0)
                playerY = 0;

            if (playerX > WorldWidth - PlayerWidth)
                playerX = WorldWidth - PlayerWidth;

            if (playerY > WorldHeight - PlayerHeight)
                playerY = WorldHeight - PlayerHeight;
        }

        // =========================
        // КАМЕРА
        // =========================

        void UpdateCamera()
        {
            cameraX =
                playerX +
                PlayerWidth / 2 -
                ClientSize.Width / 2;

            cameraY =
                playerY +
                PlayerHeight / 2 -
                ClientSize.Height / 2;

            if (cameraX < 0)
                cameraX = 0;

            if (cameraY < 0)
                cameraY = 0;

            if (cameraX >
                WorldWidth - ClientSize.Width)
            {
                cameraX =
                    WorldWidth - ClientSize.Width;
            }

            if (cameraY >
                WorldHeight - ClientSize.Height)
            {
                cameraY =
                    WorldHeight - ClientSize.Height;
            }
        }

        // =========================
        // НАЖАТИЯ КЛАВИШ
        // =========================

        void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (mainMenu)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    mainMenu = false;

                    playerX = 1450;
                    playerY = 950;

                    UpdateCamera();
                    Invalidate();
                }

                if (e.KeyCode == Keys.Escape)
                {
                    Close();
                }

                return;
            }
            // =========================
            // E
            // =========================

            if (e.KeyCode == Keys.E)
            {
                HandleInteraction();
                return;
            }

            // =========================
            // АТАКА ИГРОКА
            // =========================

            if (e.KeyCode == Keys.Space)
            {
                if (fightStarted &&
                    enemyAlive &&
                    IsNearEnemy())
                {
                    enemyHealth -= 20;

                    if (enemyHealth <= 0)
                    {
                        enemyHealth = 0;
                        enemyAlive = false;
                        fightStarted = false;

                        // Враг погибает и говорит последнюю фразу
                        enemyDialogueOpen = true;
                        enemyDialogueStep = 2;
                        dialogueOpen = true;
                    }

                    Invalidate();

                    return;
                }
            }
            // =========================
            // ВОЗРОЖДЕНИЕ
            // =========================

            if (e.KeyCode == Keys.R && playerDead)
            {
                playerDead = false;

                playerHealth = MaxPlayerHealth;

                playerX = 1450;
                playerY = 950;

                enemyAttackCooldown = 0;

                Invalidate();

                return;
            }
            // Если открыт диалог,
            // движение запрещено
            if (dialogueOpen)
                return;
            if (playerDead)
                return;

            // =========================
            // ДВИЖЕНИЕ
            // =========================

            if (e.KeyCode == Keys.W ||
                e.KeyCode == Keys.Up)
            {
                up = true;

                currentPlayerImage = playerBack;
            }

            if (e.KeyCode == Keys.S ||
                e.KeyCode == Keys.Down)
            {
                down = true;

                currentPlayerImage = playerFront;
            }

            if (e.KeyCode == Keys.A ||
                e.KeyCode == Keys.Left)
            {
                left = true;

                currentPlayerImage = playerLeft;
            }

            if (e.KeyCode == Keys.D ||
                e.KeyCode == Keys.Right)
            {
                right = true;

                currentPlayerImage = playerRight;
            }
        }

        // =========================
        // ОТПУСКАНИЕ КЛАВИШ
        // =========================

        void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W ||
                e.KeyCode == Keys.Up)
            {
                up = false;
            }

            if (e.KeyCode == Keys.S ||
                e.KeyCode == Keys.Down)
            {
                down = false;
            }

            if (e.KeyCode == Keys.A ||
                e.KeyCode == Keys.Left)
            {
                left = false;
            }

            if (e.KeyCode == Keys.D ||
                e.KeyCode == Keys.Right)
            {
                right = false;
            }
        }

        // =========================
        // ВЗАИМОДЕЙСТВИЕ
        // =========================

        void HandleInteraction()
        {
            // =========================
            // ДИАЛОГ С ВРАГОМ
            // =========================

            if (enemyDialogueOpen)
            {
                // Первая фраза врага
                if (enemyDialogueStep == 0)
                {
                    enemyDialogueStep = 1;

                    Invalidate();

                    return;
                }

                // Ответ героя
                if (enemyDialogueStep == 1)
                {
                    enemyDialogueOpen = false;
                    dialogueOpen = false;

                    fightStarted = true;

                    Invalidate();

                    return;
                }
               
                // =========================
                // ПОСЛЕДНЯЯ ФРАЗА ВРАГА
                // =========================

                if (enemyDialogueStep == 2)
                {
                    enemyDialogueOpen = false;
                    dialogueOpen = false;

                    Invalidate();

                    return;
                }
            }

            // =========================
            // ОТКРЫТЬ ДИАЛОГ С ВРАГОМ
            // =========================

            if (enemyAlive &&
                !fightStarted &&
                IsNearEnemy())
            {
                enemyDialogueOpen = true;
                enemyDialogueStep = 0;
                dialogueOpen = true;

                Invalidate();

                return;
            }

            // =========================
            // ЗАКРЫТЬ ОБЫЧНЫЙ ДИАЛОГ
            // =========================

            if (dialogueOpen)
            {
                dialogueOpen = false;

                if (!questStarted)
                {
                    questStarted = true;
                }

                Invalidate();

                return;
            }

            // =========================
            // СТАРТ / ЗАВЕРШЕНИЕ КВЕСТА
            // =========================

            if (IsNearNpc())
            {
                // Первый разговор со Странником
                if (!questStarted)
                {
                    dialogueOpen = true;

                    Invalidate();

                    return;
                }

                // Если собраны все яйца
                if (!questCompleted &&
                    eggsCollected >= RequiredEggs)
                {
                    questCompleted = true;
                    dialogueOpen = true;

                    Invalidate();

                    return;
                }

                // Обычный разговор после начала квеста
                dialogueOpen = true;

                Invalidate();

                return;
            }

            // =========================
            // ПОДБОР ЯЙЦА
            // =========================

            if (questStarted &&
                !questCompleted)
            {
                Egg nearbyEgg = GetNearbyEgg();

                if (nearbyEgg != null)
                {
                    CollectEgg(nearbyEgg);

                    return;
                }
            }
        }




        // =========================
        // ПРОВЕРКА NPC
        // =========================

        bool IsNearNpc()
        {
            float npcCenterX =
                npcX + NpcWidth / 2;

            float npcCenterY =
                npcY + NpcHeight / 2;

            float playerCenterX =
                playerX + PlayerWidth / 2;

            float playerCenterY =
                playerY + PlayerHeight / 2;

            float dx =
                playerCenterX - npcCenterX;

            float dy =
                playerCenterY - npcCenterY;

            float distance =
                (float)Math.Sqrt(
                    dx * dx +
                    dy * dy
                );

            return distance < 150;
        }
        bool IsNearEnemy()
        {
            float playerCenterX =
                playerX + PlayerWidth / 2f;

            float playerCenterY =
                playerY + PlayerHeight / 2f;

            float enemyCenterX =
                enemyX + EnemyWidth / 2f;

            float enemyCenterY =
                enemyY + EnemyHeight / 2f;

            float dx =
                playerCenterX - enemyCenterX;

            float dy =
                playerCenterY - enemyCenterY;

            float distance =
                MathF.Sqrt(dx * dx + dy * dy);

            return distance < 140;
        }

        // =========================
        // ПОИСК БЛИЖАЙШЕГО ЯЙЦА
        // =========================

        Egg GetNearbyEgg()
        {
            Egg closestEgg = null;

            float closestDistance = float.MaxValue;

            float playerCenterX =
                playerX + PlayerWidth / 2;

            float playerCenterY =
                playerY + PlayerHeight / 2;

            foreach (Egg egg in eggs)
            {
                if (egg.Collected)
                    continue;
                if (egg.Guarded && enemyAlive)
                    continue;

                float eggCenterX =
                    egg.X + EggWidth / 2;

                float eggCenterY =
                    egg.Y + EggHeight / 2;

                float dx =
                    playerCenterX - eggCenterX;

                float dy =
                    playerCenterY - eggCenterY;

                float distance =
                    (float)Math.Sqrt(
                        dx * dx +
                        dy * dy
                    );

                if (distance < 100 &&
                    distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEgg = egg;
                }
            }

            return closestEgg;
        }

        // =========================
        // СОБРАТЬ ЯЙЦО
        // =========================

        void CollectEgg(Egg egg)
        {
            egg.Collected = true;

            eggsCollected++;

            if (eggsCollected >= RequiredEggs)
            {
                eggsCollected = RequiredEggs;
            }

            Invalidate();
        }

        // =========================
        // РИСОВАНИЕ
        // =========================

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            g.SmoothingMode =
                SmoothingMode.AntiAlias;

            if (mainMenu)
            {
                DrawMainMenu(g);
                return;
            }

            // =========================
            // ФОН МИРА
            // =========================

            g.Clear(Color.FromArgb(
                104,
                170,
                90
            ));

            // =========================
            // СЕТКА ТРАВЫ
            // =========================

            DrawGrass(g);

            // =========================
            // ДОРОГИ
            // =========================

            DrawPaths(g);

            // =========================
            // ДЕРЕВЬЯ
            // =========================

            DrawTrees(g);

            // =========================
            // КАМНИ
            // =========================

            DrawRocks(g);

            // =========================
            // ЦВЕТЫ
            // =========================

            DrawFlowers(g);

            // =========================
            // ПРЕПЯТСТВИЯ
            // =========================

            foreach (Rectangle obstacle in obstacles)
            {
                Rectangle screenRect =
                    WorldToScreen(obstacle);

                using (Brush brush =
                    new SolidBrush(
                        Color.FromArgb(
                            110,
                            90,
                            70
                        )))
                {
                    g.FillRectangle(
                        brush,
                        screenRect
                    );
                }
            }

            // =========================
            // ЯЙЦА
            // =========================

            DrawEggs(g);

            // =========================
            // NPC
            // =========================

            DrawNpc(g);

            // =========================
            // ИГРОК
            // =========================

            DrawEnemy(g);
            //vrag

            DrawPlayer(g);

            // =========================
            // ПОДСКАЗКИ
            // =========================

            DrawInteractionHint(g);

            // =========================
            // HUD КВЕСТА
            // =========================

            DrawQuestHud(g);
            DrawHealthBar(g);

            // =========================
            // ДИАЛОГ
            // =========================

            if (dialogueOpen)
            {
                DrawDialogue(g);
            }
            if (playerDead)
            {
                DrawDeathScreen(g);
            }
        }
        // =========================
        // КЛИКИ МЫШКОЙ ПО МЕНЮ
        // =========================

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (!mainMenu)
                return;

            // =========================
            // ИГРАТЬ
            // =========================

            Rectangle playButton =
                new Rectangle(
                    ClientSize.Width / 2 - 150,
                    270,
                    300,
                    55
                );

            if (playButton.Contains(e.Location))
            {
                mainMenu = false;

                playerX = 1450;
                playerY = 1250;

                UpdateCamera();
                Invalidate();

                return;
            }

            // =========================
            // ОБ ИГРЕ
            // =========================

            Rectangle aboutButton =
                new Rectangle(
                    ClientSize.Width / 2 - 150,
                    350,
                    300,
                    55
                );

            if (aboutButton.Contains(e.Location))
            {
                MessageBox.Show(
                    "Кирюха: В поисках яиц\n\n" +
                    "Собери все яйца, разбросанные по миру.\n" +
                    "Исследуй карту, общайся с персонажами\n" +
                    "и сражайся с врагами.",
                    "Об игре",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                return;
            }

            // =========================
            // ВЫХОД
            // =========================

            Rectangle exitButton =
                new Rectangle(
                    ClientSize.Width / 2 - 150,
                    430,
                    300,
                    55
                );

            if (exitButton.Contains(e.Location))
            {
                Close();

                return;
            }
        }
        // =========================
        // ЭКРАН СМЕРТИ
        // =========================
        void DrawMainMenu(Graphics g)
        {
            // Фон
            g.Clear(Color.FromArgb(25, 25, 30));

            // Затемнение
            using (Brush background = new SolidBrush(
                Color.FromArgb(255, 25, 25, 30)))
            {
                g.FillRectangle(
                    background,
                    0,
                    0,
                    ClientSize.Width,
                    ClientSize.Height
                );
            }

            // Название игры
            using (Font titleFont = new Font(
                "Arial",
                42,
                FontStyle.Bold))
            {
                string title = "КИРЮХА";

                SizeF size = g.MeasureString(
                    title,
                    titleFont
                );

                float x =
                    ClientSize.Width / 2 -
                    size.Width / 2;

                g.DrawString(
                    title,
                    titleFont,
                    Brushes.White,
                    x,
                    120
                );
            }

            // Подзаголовок
            using (Font subtitleFont = new Font(
                "Arial",
                16,
                FontStyle.Regular))
            {
                string subtitle = "В поисках яиц";

                SizeF size = g.MeasureString(
                    subtitle,
                    subtitleFont
                );

                float x =
                    ClientSize.Width / 2 -
                    size.Width / 2;

                g.DrawString(
                    subtitle,
                    subtitleFont,
                    Brushes.LightGray,
                    x,
                    180
                );
            }

            DrawMenuButton(
                g,
                "ИГРАТЬ",
                270
            );

            DrawMenuButton(
                g,
                "ОБ ИГРЕ",
                350
            );

            DrawMenuButton(
                g,
                "ВЫХОД",
                430
            );
        }
        void DrawMenuButton(
    Graphics g,
    string text,
    int y)
        {
            int width = 300;
            int height = 55;

            int x =
                ClientSize.Width / 2 -
                width / 2;

            using (Brush background =
                new SolidBrush(
                    Color.FromArgb(50, 50, 60)))
            {
                g.FillRectangle(
                    background,
                    x,
                    y,
                    width,
                    height
                );
            }

            using (Pen border =
                new Pen(Color.White, 2))
            {
                g.DrawRectangle(
                    border,
                    x,
                    y,
                    width,
                    height
                );
            }

            using (Font font =
                new Font(
                    "Arial",
                    18,
                    FontStyle.Bold))
            {
                SizeF size =
                    g.MeasureString(
                        text,
                        font
                    );

                float textX =
                    ClientSize.Width / 2 -
                    size.Width / 2;

                float textY =
                    y +
                    height / 2 -
                    size.Height / 2;

                g.DrawString(
                    text,
                    font,
                    Brushes.White,
                    textX,
                    textY
                );
            }
        }
        void DrawDeathScreen(Graphics g)
        {
            using (Brush background =
                new SolidBrush(
                    Color.FromArgb(190, 0, 0, 0)))
            {
                g.FillRectangle(
                    background,
                    0,
                    0,
                    ClientSize.Width,
                    ClientSize.Height
                );
            }

            using (Font titleFont =
                new Font(
                    "Arial",
                    42,
                    FontStyle.Bold))
            {
                using (Brush textBrush =
                    new SolidBrush(Color.White))
                {
                    string text = "ВЫ ПОГИБЛИ";

                    SizeF size =
                        g.MeasureString(
                            text,
                            titleFont
                        );

                    g.DrawString(
                        text,
                        titleFont,
                        textBrush,
                        (ClientSize.Width - size.Width) / 2,
                        230
                    );
                }
            }

            using (Font hintFont =
                new Font(
                    "Arial",
                    20,
                    FontStyle.Regular))
            {
                using (Brush textBrush =
                    new SolidBrush(Color.White))
                {
                    string text =
                        "Нажмите R, чтобы возродиться";

                    SizeF size =
                        g.MeasureString(
                            text,
                            hintFont
                        );

                    g.DrawString(
                        text,
                        hintFont,
                        textBrush,
                        (ClientSize.Width - size.Width) / 2,
                        310
                    );
                }
            }
        }

        // =========================
        // РИСОВАНИЕ ТРАВЫ
        // =========================

        void DrawGrass(Graphics g)
        {
            Random random =
                new Random(12345);

            using (Pen pen =
                new Pen(
                    Color.FromArgb(
                        60,
                        130,
                        60
                    ),
                    1
                ))
            {
                for (int i = 0; i < 1500; i++)
                {
                    int x =
                        random.Next(
                            0,
                            WorldWidth
                        );

                    int y =
                        random.Next(
                            0,
                            WorldHeight
                        );

                    int screenX =
                        (int)(x - cameraX);

                    int screenY =
                        (int)(y - cameraY);

                    if (screenX < -10 ||
                        screenX > ClientSize.Width + 10 ||
                        screenY < -10 ||
                        screenY > ClientSize.Height + 10)
                    {
                        continue;
                    }

                    g.DrawLine(
                        pen,
                        screenX,
                        screenY,
                        screenX + 3,
                        screenY - 5
                    );
                }
            }
        }

        // =========================
        // ДОРОГИ
        // =========================

        void DrawPaths(Graphics g)
        {
            using (Pen pen =
                new Pen(
                    Color.FromArgb(
                        180,
                        150,
                        100
                    ),
                    80
                ))
            {
                pen.StartCap =
                    LineCap.Round;

                pen.EndCap =
                    LineCap.Round;

                g.DrawLine(
                    pen,
                    (int)(0 - cameraX),
                    (int)(1000 - cameraY),
                    (int)(3000 - cameraX),
                    (int)(1000 - cameraY)
                );

                g.DrawLine(
                    pen,
                    (int)(1500 - cameraX),
                    (int)(0 - cameraY),
                    (int)(1500 - cameraX),
                    (int)(2000 - cameraY)
                );
            }
        }

        // =========================
        // ДЕРЕВЬЯ
        // =========================

        void DrawTrees(Graphics g)
        {
            Point[] trees =
            {
                new Point(200,200),
                new Point(400,400),
                new Point(800,200),
                new Point(2300,250),
                new Point(2700,400),
                new Point(500,1500),
                new Point(2400,1600),
                new Point(2700,1800)
            };

            foreach (Point tree in trees)
            {
                int x =
                    (int)(tree.X - cameraX);

                int y =
                    (int)(tree.Y - cameraY);

                using (Brush trunk =
                    new SolidBrush(
                        Color.SaddleBrown))
                {
                    g.FillRectangle(
                        trunk,
                        x + 25,
                        y + 45,
                        20,
                        50
                    );
                }

                using (Brush crown =
                    new SolidBrush(
                        Color.DarkGreen))
                {
                    g.FillEllipse(
                        crown,
                        x,
                        y,
                        70,
                        70
                    );
                }
            }
        }

        // =========================
        // КАМНИ
        // =========================

        void DrawRocks(Graphics g)
        {
            Point[] rocks =
            {
                new Point(350,550),
                new Point(750,850),
                new Point(1300,450),
                new Point(1900,350),
                new Point(2600,700),
                new Point(700,1700),
                new Point(2100,1700)
            };

            using (Brush brush =
                new SolidBrush(
                    Color.Gray))
            {
                foreach (Point rock in rocks)
                {
                    int x =
                        (int)(rock.X - cameraX);

                    int y =
                        (int)(rock.Y - cameraY);

                    g.FillEllipse(
                        brush,
                        x,
                        y,
                        45,
                        30
                    );
                }
            }
        }

        // =========================
        // ЦВЕТЫ
        // =========================

        void DrawFlowers(Graphics g)
        {
            Point[] flowers =
            {
                new Point(450,250),
                new Point(850,500),
                new Point(1250,700),
                new Point(1750,300),
                new Point(2600,500),
                new Point(400,1250),
                new Point(1750,1300),
                new Point(2700,1500)
            };

            foreach (Point flower in flowers)
            {
                int x =
                    (int)(flower.X - cameraX);

                int y =
                    (int)(flower.Y - cameraY);

                using (Brush brush =
                    new SolidBrush(
                        Color.Pink))
                {
                    g.FillEllipse(
                        brush,
                        x,
                        y,
                        8,
                        8
                    );
                }

                using (Brush brush =
                    new SolidBrush(
                        Color.Yellow))
                {
                    g.FillEllipse(
                        brush,
                        x + 3,
                        y + 3,
                        4,
                        4
                    );
                }
            }
        }

        // =========================
        // РИСОВАНИЕ ЯИЦ
        // =========================

        void DrawEggs(Graphics g)
        {
            foreach (Egg egg in eggs)
            {
                if (egg.Collected)
                    continue;

                int x =
                    (int)(egg.X - cameraX);

                int y =
                    (int)(egg.Y - cameraY);

                // Если яйцо далеко за экраном,
                // его можно не рисовать.
                if (x < -EggWidth ||
                    x > ClientSize.Width + EggWidth ||
                    y < -EggHeight ||
                    y > ClientSize.Height + EggHeight)
                {
                    continue;
                }

                // Тень
                using (Brush shadow =
                    new SolidBrush(
                        Color.FromArgb(
                            70,
                            0,
                            0,
                            0
                        )))
                {
                    g.FillEllipse(
                        shadow,
                        x - 3,
                        y + 30,
                        38,
                        10
                    );
                }

                // Яйцо
                using (Brush eggBrush =
                    new SolidBrush(
                        Color.WhiteSmoke))
                {
                    g.FillEllipse(
                        eggBrush,
                        x,
                        y,
                        EggWidth,
                        EggHeight
                    );
                }

                // Контур
                using (Pen outline =
                    new Pen(
                        Color.LightGray,
                        2))
                {
                    g.DrawEllipse(
                        outline,
                        x,
                        y,
                        EggWidth,
                        EggHeight
                    );
                }

                // Маленькое пятнышко
                using (Brush spot =
                    new SolidBrush(
                        Color.Beige))
                {
                    g.FillEllipse(
                        spot,
                        x + 8,
                        y + 10,
                        7,
                        7
                    );
                }

                // Второе пятнышко
                using (Brush spot =
                    new SolidBrush(
                        Color.Wheat))
                {
                    g.FillEllipse(
                        spot,
                        x + 18,
                        y + 23,
                        5,
                        5
                    );
                }
            }
        }
        

        // =========================
        // NPC
        // =========================

        void DrawNpc(Graphics g)
        {
            int x = (int)(npcX - cameraX);
            int y = (int)(npcY - cameraY);

            g.DrawImage(
                npcImage,
                new Rectangle(
                    x,
                    y,
                    NpcWidth,
                    NpcHeight
                )
            );
        }
        void DrawEnemy(Graphics g)
        {
            if (!enemyAlive)
                return;

            int x = (int)(enemyX - cameraX);
            int y = (int)(enemyY - cameraY);

            g.DrawImage(
                enemyImage,
                new Rectangle(
                    x,
                    y,
                    EnemyWidth,
                    EnemyHeight
                )
            );

            // Полоска здоровья врага
            int barWidth = EnemyWidth;
            int barHeight = 7;

            using (Brush background =
                new SolidBrush(Color.DarkRed))
            {
                g.FillRectangle(
                    background,
                    x,
                    y - 12,
                    barWidth,
                    barHeight
                );
            }

            float healthPercent =
                (float)enemyHealth / MaxEnemyHealth;

            int healthWidth =
                (int)(barWidth * healthPercent);

            if (healthWidth > 0)
            {
                using (Brush health =
                    new SolidBrush(Color.LimeGreen))
                {
                    g.FillRectangle(
                        health,
                        x,
                        y - 12,
                        healthWidth,
                        barHeight
                    );
                }
            }
        }
        // =========================
        // ИГРОК
        // =========================

        void DrawPlayer(Graphics g)
        {
            int x =
                (int)(playerX - cameraX);

            int y =
                (int)(playerY - cameraY);

            g.DrawImage(
                currentPlayerImage,
                new Rectangle(
                    x,
                    y,
                    PlayerWidth,
                    PlayerHeight
                )
            );
        }

        // =========================
        // ПОДСКАЗКА
        // =========================

        void DrawInteractionHint(Graphics g)
        {
            if (dialogueOpen)
                return;

            string hint = null;

            if (IsNearNpc())
            {
                if (!questStarted)
                {
                    hint = "E — поговорить";
                }
                else if (!questCompleted &&
                         eggsCollected >= RequiredEggs)
                {
                    hint = "E — отдать яйца";
                }
                else
                {
                    hint = "E — поговорить";
                }
            }
            else if (questStarted &&
                     !questCompleted &&
                     GetNearbyEgg() != null)
            {
                hint = "E — собрать яйцо";
            }

            if (hint == null)
                return;

            using (Font font =
                new Font(
                    "Arial",
                    12,
                    FontStyle.Bold))
            {
                SizeF size =
                    g.MeasureString(
                        hint,
                        font
                    );

                int x =
                    ClientSize.Width / 2 -
                    (int)size.Width / 2;

                int y =
                    ClientSize.Height - 70;

                using (Brush background =
                    new SolidBrush(
                        Color.FromArgb(
                            200,
                            0,
                            0,
                            0
                        )))
                {
                    g.FillRectangle(
                        background,
                        x - 10,
                        y - 5,
                        (int)size.Width + 20,
                        (int)size.Height + 10
                    );
                }

                using (Brush text =
                    new SolidBrush(
                        Color.White))
                {
                    g.DrawString(
                        hint,
                        font,
                        text,
                        x,
                        y
                    );
                }
            }
        }

        // =========================
        // HUD КВЕСТА
        // =========================

        void DrawQuestHud(Graphics g)
        {
            int x = 20;
            int y = 65;

            int width = 360;
            int height = 105;

            // Фон HUD
            using (Brush background =
                new SolidBrush(Color.FromArgb(210, 0, 0, 0)))
            {
                g.FillRectangle(
                    background,
                    x,
                    y,
                    width,
                    height
                );
            }

            // Рамка
            using (Pen border =
                new Pen(Color.White, 2))
            {
                g.DrawRectangle(
                    border,
                    x,
                    y,
                    width,
                    height
                );
            }

            using (Font titleFont =
                new Font(
                    "Arial",
                    14,
                    FontStyle.Bold))
            {
                using (Brush textBrush =
                    new SolidBrush(Color.White))
                {
                    // Заголовок
                    string title;

                    if (questCompleted)
                    {
                        title = "✓ ЗАДАНИЕ ВЫПОЛНЕНО";
                    }
                    else
                    {
                        title = "ЗАДАНИЕ";
                    }

                    g.DrawString(
                        title,
                        titleFont,
                        textBrush,
                        x + 15,
                        y + 10
                    );
                }
            }

            using (Font questFont =
                new Font(
                    "Arial",
                    12,
                    FontStyle.Regular))
            {
                using (Brush textBrush =
                    new SolidBrush(Color.White))
                {
                    string questText;

                    if (!questStarted)
                    {
                        questText =
                            "Поговори с Тимохой";
                    }
                    else if (!questCompleted)
                    {
                        questText =
                            "Собери яйца: " +
                            eggsCollected +
                            " / " +
                            RequiredEggs;
                    }
                    else
                    {
                        questText =
                            "Все яйца собраны!";
                    }

                    g.DrawString(
                        questText,
                        questFont,
                        textBrush,
                        x + 15,
                        y + 45
                    );

                    // Вторая строка
                    string secondLine;

                    if (!questStarted)
                    {
                        secondLine = "Нажми E рядом с ним";
                    }
                    else if (!questCompleted)
                    {
                        secondLine =
                            "Найди все " +
                            RequiredEggs +
                            " яиц";
                    }
                    else
                    {
                        secondLine = "Квест завершён";
                    }

                    g.DrawString(
                        secondLine,
                        questFont,
                        textBrush,
                        x + 15,
                        y + 70
                    );
                }
            }
        }

        void DrawHealthBar(Graphics g)
        {
            int x = 20;
            int y = 20;

            int width = 220;
            int height = 25;

            // Фон полоски
            using (Brush background =
                new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
            {
                g.FillRectangle(
                    background,
                    x,
                    y,
                    width,
                    height
                );
            }

            // Зелёная часть здоровья
            float healthPercent =
                (float)playerHealth / MaxPlayerHealth;

            int healthWidth =
                (int)(width * healthPercent);

            if (healthWidth > 0)
            {
                using (Brush health =
                    new SolidBrush(Color.LimeGreen))
                {
                    g.FillRectangle(
                        health,
                        x,
                        y,
                        healthWidth,
                        height
                    );
                }
            }

            // Рамка
            using (Pen border =
                new Pen(Color.White, 2))
            {
                g.DrawRectangle(
                    border,
                    x,
                    y,
                    width,
                    height
                );
            }

            // Текст
            using (Font font =
                new Font(
                    "Arial",
                    11,
                    FontStyle.Bold))
            {
                string text =
                    "❤️ " +
                    playerHealth +
                    " / " +
                    MaxPlayerHealth;

                using (Brush textBrush =
                    new SolidBrush(Color.White))
                {
                    g.DrawString(
                        text,
                        font,
                        textBrush,
                        x + 65,
                        y + 3
                    );
                }
            }
        }

        // =========================
        // ДИАЛОГ
        // =========================

        void DrawDialogue(Graphics g)
        {
            // Диалог с врагом
            if (enemyDialogueOpen)
            {
                using (Brush background =
                    new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
                {
                    g.FillRectangle(
                        background,
                        50,
                        ClientSize.Height - 180,
                        ClientSize.Width - 100,
                        120
                    );
                }

                using (Pen border =
                    new Pen(Color.White, 2))
                {
                    g.DrawRectangle(
                        border,
                        50,
                        ClientSize.Height - 180,
                        ClientSize.Width - 100,
                        120
                    );
                }

                string enemyDialogueText;

                if (enemyDialogueStep == 0)
                {
                    // Первая фраза врага
                    enemyDialogueText = "Кирюха депай";
                }
                else if (enemyDialogueStep == 1)
                {
                    // Ответ героя
                    enemyDialogueText = "Ну уж нет, хрен тебе, драные кейсики.";
                }
                else
                {
                    // Последняя фраза врага перед смертью
                    enemyDialogueText = "Последнего додепа не будет.";
                }

                using (Font font =
                    new Font(
                        "Arial",
                        18,
                        FontStyle.Bold))
                {
                    using (Brush textBrush =
                        new SolidBrush(Color.White))
                    {
                        g.DrawString(
                            enemyDialogueText,
                            font,
                            textBrush,
                            80,
                            ClientSize.Height - 140
                        );
                    }
                }

                return;
            }
            int boxHeight = 170;

            Rectangle box =
                new Rectangle(
                    40,
                    ClientSize.Height -
                    boxHeight -
                    30,
                    ClientSize.Width - 80,
                    boxHeight
                );

            using (Brush background =
                new SolidBrush(
                    Color.FromArgb(
                        235,
                        20,
                        20,
                        25
                    )))
            {
                g.FillRectangle(
                    background,
                    box
                );
            }

            using (Pen border =
                new Pen(
                    Color.White,
                    2))
            {
                g.DrawRectangle(
                    border,
                    box
                );
            }

            string name =
                "Тимоха";

            string text;

            // =========================
            // ПЕРВЫЙ ДИАЛОГ
            // =========================

            if (!questStarted)
            {
                text =
                    "Здарова, Кирюха\n\n" +
                    "Только ты мне можешь в этом помочь. " +
                    "Собери для меня " +
                    RequiredEggs +
                    " яиц.\n\n" +
                    "Они разбросаны по всему миру.";

            }

            // =========================
            // КВЕСТ АКТИВЕН
            // =========================

            else if (!questCompleted &&
                     eggsCollected < RequiredEggs)
            {
                text =
                    "Где мои яйца бля.\n\n" +
                    "Продолжай поиски.\n" +
                    "Собрано: " +
                    eggsCollected +
                    " / " +
                    RequiredEggs;
            }

            // =========================
            // ВСЕ ЯЙЦА СОБРАНЫ
            // =========================

            else if (!questCompleted &&
                     eggsCollected >= RequiredEggs)
            {
                text =
                    "Пасиба, Кирюха\n\n" +
                    "Ты собрал все " +
                    RequiredEggs +
                    " яиц.\n" +
                    "Да благословит тебя бог!";
            }

            // =========================
            // КВЕСТ ЗАВЕРШЁН
            // =========================

            else
            {
                text =
                    "Спасибо тебе ещё раз, Кирюха\n\n" +
                    "Ты ловко нашёл мои яйца.";
            }

            using (Font nameFont =
                new Font(
                    "Arial",
                    16,
                    FontStyle.Bold))
            {
                using (Brush brush =
                    new SolidBrush(
                        Color.Gold))
                {
                    g.DrawString(
                        name,
                        nameFont,
                        brush,
                        box.X + 20,
                        box.Y + 15
                    );
                }
            }

            using (Font textFont =
                new Font(
                    "Arial",
                    13))
            {
                using (Brush brush =
                    new SolidBrush(
                        Color.White))
                {
                    g.DrawString(
                        text,
                        textFont,
                        brush,
                        box.X + 20,
                        box.Y + 50
                    );
                }
            }

            using (Font hintFont =
                new Font(
                    "Arial",
                    10,
                    FontStyle.Italic))
            {
                using (Brush brush =
                    new SolidBrush(
                        Color.LightGray))
                {
                    g.DrawString(
                        "E — закрыть",
                        hintFont,
                        brush,
                        box.X + 20,
                        box.Bottom - 25
                    );
                }
            }
        }

        // =========================
        // МИР → ЭКРАН
        // =========================

        Rectangle WorldToScreen(
            Rectangle worldRectangle)
        {
            return new Rectangle(
                (int)(worldRectangle.X - cameraX),
                (int)(worldRectangle.Y - cameraY),
                worldRectangle.Width,
                worldRectangle.Height
            );
        }
    }
}