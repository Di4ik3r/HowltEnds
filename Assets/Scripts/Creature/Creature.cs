using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum Direction {
    UP,
    RIGHT,
    DOWN,
    LEFT,
    Z_UNAVAILABLE
}

public enum CreatureState {
    Standing,
    Walking,
    LootingFood,
    Eating,
    LootingWater,
    Drinking,
    Died
}

public class Creature {

    // Основне поле класу, яке тримає в собі основні компоненти Юніті (такі як Трансформ) і куда можна
    // добавити нові компоненти (такі як МешРендерер, МешФільтр і тд)
    private GameObject gameObject;
    // Властивість, яка дозволяє звертатись до Трансформ геймОбджекта даного класу
    public Transform transform { get{ return this.gameObject.transform; } }
    // Властивість, яка дозволяє звертатись до Позиції геймОбджекта даного класу
    public Vector3 position { get{ return this.gameObject.transform.position; }  set { this.gameObject.transform.position = value; } }

    // Статичне поле даного класу, яке дозволяє відслідковувати і обробляти ідентифікатори для кожного
    // юніта
    public static int ID_COUNTER = 0;
    // Статичне поле даного класу, яке дозволяє тримати ІНТовський масив карти
    public static int[,] digitalMap;
    // Статичне поле даного класу, яке дозволяє тримати масив кубів карти    
    public static GameObject[,] objectMap;

    // Статичне поле даного класу, яке дозволяє визначити мінімальну і максимальну кількість днів в межах
    // якої буде обиратись скільки Юніт проживе (в днях)
    private static Vector2 DEATH_RANGE = new Vector2(6, 15);
    // Ліміт, при досягненні якого - Юніт буде шукати їжу. Крок, який буде додаватись до загального показника
    // голоду
    private static float HUNGER_LIMIT = .6f, HUNGER_STEP = .08f;
    // Ліміт, при досягненні якого - Юніт буде шукати воду. Крок, який буде додаватись до загального показника
    // спраги
    private static float THIRST_LIMIT = .4f, THIRST_STEP = .06f;

    // Ідентифікатор Юніта
    private int     id;
    
    private CreatureState state;

    // Показник голоду
    private float   hunger;
    // Показник спраги
    private float   thirst;
    // День, коли Юніт був створений
    private int     birthDay;
    // День, коли юніт помре природньою смертю (День народження + Рандом.Рендж(ДЕАД_РЕНДЖ.х, ДЕАД_РЕНДЖ.у))
    private int     deathDay;
    // Швидкість
    private float   speed;
    // Маса
    private float   weight;
    // Відображає чи Юніт рухається
    // private bool    isMoving;
    // Клітка мапи, до якої прямує Юніт, якщо він відчуває голод або спрагу ((-1, -1), якщо ні до чого не прямує)
    private Vector2[] targetPath;
    private int pathIndex;
    private bool PathIsValid {
        get { return targetPath != null && targetPath.Length > 0 && pathIndex >= 0 && pathIndex < targetPath.Length; }
        // get { return targetPath != null && targetPath.Length > 0 && pathIndex == 0; }
    }
    // private bool TargetCellIsValid {
    //     get { return (targetCell.x >= 0 && targetCell.x < digitalMap.GetLength(0) && targetCell.y >= 0 && targetCell.y < digitalMap.GetLength(1)); }
    // }

    // Меш для Юніта (його завнішній вигляд)
    private Mesh mesh;
    // Висота Меша. Потрібно для того щоб юніт правильно ставав на блоки по висоті
    private float meshHeight;

    // Властивість, що відображає чи відчуває голод Юніт
    private bool isHunger { get { return this.hunger >= Creature.HUNGER_LIMIT ? true : false; } }
    // Властивість, що відображає чи відчуває спрагу Юніт
    private bool isThirst { get { return this.thirst >= Creature.THIRST_LIMIT ? true : false; } }
    
    // Напрямок в якому буде рухатись Юніт
    private Direction moveDirection;
    // Список кліток даного напрямку
    private List<Vector2> directionChoices;
    // Випадквий ліміт ходів в даному напрямку та кількість вже зроблених кроків в даному напрямку
    private int limitMoveCount, currentMoveCount;
    // Статичний масив можливих напрямків
    private static Direction[] DIRECTIONS = {Direction.UP, Direction.RIGHT, Direction.DOWN, Direction.LEFT};
    // Ліміт максимально можливих ходів в даному напрямку
    private static int MOVES_LIMIT_MIN = 3;
    private static int MOVES_LIMIT_MAX = 12;

    public Creature(Vector2 position, int birthDay) {
        // За допомогою стягнутого класу ПрімітівХелпер - стягує меш з куба
        this.mesh = PrimitiveHelper.GetPrimitiveMesh(PrimitiveType.Cube);
        // Фукнція, що додає до геймОбджекта потрібні нам компоненти і правильно інціалізує їх
        ImplementComponents();

        // Ініціалізація полів
        this.id = Creature.ID_COUNTER++;
        this.hunger = 0;
        this.thirst = 0;
        this.birthDay = birthDay;
        this.deathDay = (int)Random.Range(DEATH_RANGE.x, DEATH_RANGE.y);
        this.speed = Random.Range(0.7f, 3); 
        this.weight = 0;
        // this.isMoving = false;
        // this.isLookingFor = false;
        // this.targetCell = new Vector2(position.x, position.y);
        this.targetPath = new Vector2[1];
        this.targetPath[0] = new Vector2(position.x, position.y);
        this.pathIndex = 0;
        // this.isAlive = true;
        this.state = CreatureState.Standing;

        this.meshHeight = 1;

        this.position = new Vector3(position.x, 
                                    Creature.objectMap[(int)position.x, (int)position.y].transform.position.y + this.meshHeight,
                                    position.y);
        // Позначення в глобальному масиві, що дана клітка зайнята
        Creature.digitalMap[(int)position.x, (int)position.y] = 9;
        // Вибір випадкового напрямку, кліток
        RandomizeMove();
    }

    // Додає до геймОбджекта потрібні нам компоненти і правильно інціалізує їх
    private void ImplementComponents() {
        // Створює екземпляр ГеймОбджекта
        this.gameObject = new GameObject("Creature");

        // Додає компонент МешФільтр, який тримає в собі меш
        this.gameObject.AddComponent<MeshFilter>();
        // Додає компонент МешРендерер, який тримає в собі параметри, потрібні для відображення
        // такі, як матеріал, колір, параметри освітлення і тд.
        this.gameObject.AddComponent<MeshRenderer>();
        
        // Присвоюю мешу компонента МешФільтра меш Юніта
        this.gameObject.GetComponent<MeshFilter>().mesh = this.mesh;
        // Присвою матеріал компоненту Рендерер
        this.gameObject.GetComponent<Renderer>().materials[0] = Resources.Load("BasicMaterial", typeof(Material)) as Material;
        this.gameObject.GetComponent<Renderer>().materials[0].color = Color.black;

        float scale = .7f;
        this.transform.localScale = new Vector3(scale, scale, scale);
    }


    // Логіка руху Юніта
    public void MakeMove() {
        // Якщо мертвий - нічого не робимо
        if(this.state == CreatureState.Died)
            return;
        
        // Якщо голодний - шукаємо їжу
        if(isHunger) {
            if(FoundFood()) {
                
            }
        }
        // Якщо Юніт відчуває спрагу - шукаємо воду
        if(isThirst) {
            if(FoundWater()) {
                
            }
        }

        // if(PathIsValid) {
        //     Debug.Log("path has started");
        //     StartMoving(this.targetPath[this.pathIndex++]);
        // }
        
        // Виконуємо рух
        Jump();
    }


    private bool FoundWater() {
        return false;
    }

    private bool FoundFood() {
        return false;
    }

    public void SetPath(Vector2[] path) {
        this.targetPath = path;
        this.pathIndex = 0;
    }

    // Рух, при виконані якого - збільшується голод та спрага
    private void Jump() {
        if(this.state == CreatureState.Walking) {
            AnimateMoving();
        }
        else {
            if(PathIsValid)
                StartMoving(this.targetPath[this.pathIndex++]);
            else StartMoving();
        }

        // При кожному ході - збільшення показників відчуття голоду та спраги
        this.hunger += HUNGER_STEP;
        this.thirst += THIRST_STEP;
    }

    public void StartMoving(Vector2 targetPosition) {
        float x = targetPosition.x;
        float z = targetPosition.y;
        float y = Creature.objectMap[(int)x, (int)z].transform.position.y + this.meshHeight;
        this.moveStartPosition = this.position;
        this.moveTargetPosition = new Vector3(x, y, z);
        this.state = CreatureState.Walking;
    }

    private void StartMoving() {
        this.moveStartPosition = this.position;
        this.moveTargetPosition = MoveLogic();
        this.state = CreatureState.Walking;
    }

    // Змінні які потрібні для анімованого руху
    private float moveTime;
    private float moveArcHeight = 1f;
    Vector3 moveStartPosition;
    public Vector2 ArrayPosition { get { return new Vector2(moveStartPosition.x, moveStartPosition.z);}}
    Vector3 moveTargetPosition;
    // Анімований рух
    void AnimateMoving () {
        moveTime = Mathf.Min (1, moveTime + Time.deltaTime * speed);
        float height = (1 - 4 * (moveTime - .5f) * (moveTime - .5f)) * moveArcHeight;
        this.position = Vector3.Lerp (moveStartPosition, moveTargetPosition, moveTime) + Vector3.up * height;

        if (moveTime >= 1) {
            moveTime = 0;
            this.state = CreatureState.Standing;
        }
    }

    // Логіка вибору клітки, на яку ступить Юніт
    private Vector3 MoveLogic() {
        // Якщо кількість кроків на даний напрямок не вичерпана або на даниий напрямок немає
        // доступних кліток
        if(this.currentMoveCount >= this.limitMoveCount || NoAvailableChioces()) {
            // перевибираємо напрямок
            RandomizeMove();
        } 
        // Інакше, перевибираємо клітки даного напрямку
        else RandomizeChioces();

        // Оголошуємо змінні, яка повернемо і запамятовуємо поточну позицію
        float   x = this.position.x,
                y = this.position.y,
                z = this.position.z;
        // Занульовуємо поточну клітку, щоб залишити її вільною для ходу, т.я. ми вибиремо нову
        Creature.digitalMap[(int)x, (int)z] = 0;

        // Випадково вибираємо клікту з можливих
        int chioce = Random.Range(0, directionChoices.Count);
        // Якщо немає доступних кліток
        if(directionChoices.Count > 0) {
            // Обираємо поточну і запамятовуємо її
            x = directionChoices[chioce].x;
            z = directionChoices[chioce].y;
            y = Creature.objectMap[(int)x, (int)z].transform.position.y + this.meshHeight;
        }

        // Позначаємо нашу позицію в глобальному масиві, щоб ніхто не зміг стати на цю позицію
        // т.я. вона вже занята
        Creature.digitalMap[(int)x, (int)z] = 9;
        // Збільшуємо індексер кроків в даному напрямку
        this.currentMoveCount++;

        // Формуємо результат
        Vector3 result = new Vector3(x, y, z);

        // Повертаємо результат
        return result;
    }

    // Перевірка на відсутність доступих кліток даного напрямку
    private bool NoAvailableChioces() {
        if(GetAvailableCellsIndexesByDirection().Count < 1)
            return true;
        return false;
    }

    // Перевірка клітки, на існування: чи індекси відповідають умовам та типу блока
    private bool CheckCell(Vector2 cell, int type) {
        // Якщо індекс Х знаходиться поза межами масиву
        if(cell.x >= 0 && cell.x < Creature.digitalMap.GetLength(0)) {
            // Якщо індекс У знаходиться поза межами масиву
            if(cell.y >= 0 && cell.y < Creature.digitalMap.GetLength(1))
                // Якщо клітка, на якому ми хочемо обрати - відповідає заданому типу
                if(Creature.digitalMap[(int)cell.x, (int)cell.y] == type)
                    return true;
        }
        return false;
    }

    // Випадково задає клітки даного напрямку
    private void RandomizeChioces() {
        this.directionChoices = GetAvailableCellsIndexesByDirection();
    }

    // Випадково обирає напрямок і задаємо змінним небохідні нам значення
    private void RandomizeMove() {
        // Вибір можливого випадкового напрямку
        this.moveDirection = GetRandomAvailableDirection();
        
        // Якщо немає доступних напрямків - виводимо про це інформацію
        if(this.moveDirection == Direction.Z_UNAVAILABLE) {
            Debug.Log("UNAVAILABLE");
        }

        // Випадкове задання ліміту кроків в даному напрямку
        this.limitMoveCount = Random.Range(MOVES_LIMIT_MIN, MOVES_LIMIT_MAX);
        // Занулення кількості вже зроблених кроків
        this.currentMoveCount = 0;

        // Вибірка списку доступних кроків по даному напрямку
        this.directionChoices = GetAvailableCellsIndexesByDirection();
    }
    
    // Формування списка доступних кліток за даним напрямком
    private List<Vector2> GetAvailableCellsIndexesByDirection() {
        // Формуємо список рузультатів
        List<Vector2> result = new List<Vector2>();
        // Стягуємо можливі клітки по даному напрямку
        List<Vector2> choices = GetCellsIndexesByDirection();

        // Пробігаємось по всім можливим кліткам
        foreach(Vector2 chioce in choices) {
            // Якщо клітка відповідає виомгам
            if(CheckCell(chioce, 0)) {
                // Додаємо її в список
                result.Add(chioce);
            }
        }

        return result;
    }

    // Формуємання списка доступних кліток за заданим намрямком
    private List<Vector2> GetAvailableCellsIndexesByDirection(Direction direction) {
        // Формуємо список рузультатів
        List<Vector2> result = new List<Vector2>();
        // Стягуємо можливі клітки по заданому напрямку
        List<Vector2> choices = GetCellsIndexesByDirection(direction);

        // Пробігаємось по всім можливим кліткам
        foreach(Vector2 chioce in choices) {
            // Якщо клітка відповідає виомгам
            if(CheckCell(chioce, 0)) {
                // Додаємо її в список
                result.Add(chioce);
            }
        }

        return result;
    }

    // Формування списка індексів за даним напрямком
    // Правила обирання кліток, за даним напрямком
    private List<Vector2> GetCellsIndexesByDirection() {
        List<Vector2> result = new List<Vector2>();

        switch(this.moveDirection) {
            case Direction.UP: {
            // Лівий верхній кут
                result.Add(new Vector2(this.position.x - 1,   this.position.z + 1));
            // Середньо-верхня клітка
                result.Add(new Vector2(this.position.x,       this.position.z + 1));
            // Правий верхній кут
                result.Add(new Vector2(this.position.x + 1,   this.position.z + 1));
                break;
            }
            case Direction.RIGHT: {
            // Правий верхній кут
                result.Add(new Vector2(this.position.x + 1,   this.position.z + 1));
            // Право-середня клітка
                result.Add(new Vector2(this.position.x + 1,   this.position.z));
            // Правий нижній кут
                result.Add(new Vector2(this.position.x + 1,   this.position.z - 1));
                break;
            }
            case Direction.DOWN: {
            // Правий нижній кут
                result.Add(new Vector2(this.position.x + 1,   this.position.z - 1));
            // Середньо-нижня клітка
                result.Add(new Vector2(this.position.x,       this.position.z - 1));
            // Лівий нижній кут
                result.Add(new Vector2(this.position.x - 1,   this.position.z - 1));
                break;
            }    
            case Direction.LEFT: {
            // Лівий нижній кут
                result.Add(new Vector2(this.position.x - 1,   this.position.z - 1));
            // Ліво-центральна клітка
                result.Add(new Vector2(this.position.x - 1,   this.position.z));
            // Лівий верхній кут
                result.Add(new Vector2(this.position.x - 1,   this.position.z + 1));
                break;
            }

        }
        return result;
    }

    // Формування списка індексів за заданим напрямком
    // Правила обирання кліток, за заданим напрямком
    private List<Vector2> GetCellsIndexesByDirection(Direction direction) {
        List<Vector2> result = new List<Vector2>();

        switch(direction) {
            case Direction.UP: {
            // Лівий верхній кут
                result.Add(new Vector2(this.position.x - 1,   this.position.z + 1));
            // Середньо-верхня клітка
                result.Add(new Vector2(this.position.x,       this.position.z + 1));
            // Правий верхній кут
                result.Add(new Vector2(this.position.x + 1,   this.position.z + 1));
                break;
            }
            case Direction.RIGHT: {
            // Правий верхній кут
                result.Add(new Vector2(this.position.x + 1,   this.position.z + 1));
            // Право-середня клітка
                result.Add(new Vector2(this.position.x + 1,   this.position.z));
            // Правий нижній кут
                result.Add(new Vector2(this.position.x + 1,   this.position.z - 1));
                break;
            }
            case Direction.DOWN: {
            // Правий нижній кут
                result.Add(new Vector2(this.position.x + 1,   this.position.z - 1));
            // Середньо-нижня клітка
                result.Add(new Vector2(this.position.x,       this.position.z - 1));
            // Лівий нижній кут
                result.Add(new Vector2(this.position.x - 1,   this.position.z - 1));
                break;
            }    
            case Direction.LEFT: {
            // Лівий нижній кут
                result.Add(new Vector2(this.position.x - 1,   this.position.z - 1));
            // Ліво-центральна клітка
                result.Add(new Vector2(this.position.x - 1,   this.position.z));
            // Лівий верхній кут
                result.Add(new Vector2(this.position.x - 1,   this.position.z + 1));
                break;
            }

        }
        return result;
    }

    // Вибір випадкового доступного напрямку
    private Direction GetRandomAvailableDirection() {
        List<Direction> availableDirections = new List<Direction>();
        for(int i = 0; i < 4; i++) {
            Direction direction = DIRECTIONS[i];
            if(GetAvailableCellsIndexesByDirection(direction).Count > 0)
                availableDirections.Add(direction);
        }
        if(availableDirections.Count < 1)
            return Direction.Z_UNAVAILABLE;
        return availableDirections[Random.Range(0, availableDirections.Count)];
    }

    // Вибір випадкового доступного напрямку (застаріла версія)
    private Direction GetRandomAvailableDirection_deprecated() {
        for(int i = 0; i < 15; i++) {
            Direction direction = GetRandomDirection();
            List<Vector2> chioces = GetCellsIndexesByDirection(direction);
            foreach(Vector2 chioce in chioces) {
                if(CheckCell(chioce, 0))
                    return direction;
            }
        }
        return Direction.Z_UNAVAILABLE;
    }

    // Вибір випадкового напрямку
    private Direction GetRandomDirection() {
        return DIRECTIONS[Random.Range(0, DIRECTIONS.Length)];
    }

    // Повертає список індексів доступних кліток, навколо Юніта
    private List<Vector2> GetAvailableCellsIndexes(int r, int type) {
        List<Vector2> result = new List<Vector2>();

        for(int x = (int)this.position.x - r; x <= this.position.x + r; x++) {
            if(x >= 0 && x < Creature.digitalMap.GetLength(0))
                for(int z = (int)this.position.z - r; z <= this.position.z + r; z++) {
                    if(x == (int)this.position.x && z == (int)this.position.z)
                        continue;
                    if(z >= 0 && z < Creature.digitalMap.GetLength(1))
                        if(Creature.digitalMap[x, z] == type)
                            result.Add(new Vector2(x, z));
                }
        }

        return result;
    }
}
