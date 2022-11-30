// Magica Cloth.
// Copyright (c) MagicaSoft, 2020-2022.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth
{
    /// <summary>
    /// 基本的なシングルトンテンプレート
    /// ・シーンに無い場合は作成する
    /// ・自動初期化呼び出し機能
    /// ・DontDestroyOnLoad設定
    /// ・実行前でもInstanceアクセス可能
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class CreateSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;

        /// <summary>
        /// 初期化フラグ
        /// </summary>
        private static T initInstance;

        private static bool isDestroy;


        /// <summary>
        /// Reload Domain 対応
        /// ※残念ながらジェネリッククラスでは[RuntimeInitializeOnLoadMethod]が利用できないため、
        /// この初期化関数を派生元で[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        /// を使用して呼び出さなければならない
        /// </summary>
        protected static void InitMember()
        {
            instance = null;
            initInstance = null;
            isDestroy = false;
        }

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    // FindObjectOfTypeはそれなりに負荷がかかるので注意！
                    // 非アクティブのオブジェクトは発見できないので注意！
                    instance = FindObjectOfType<T>();

                    if (instance == null && Application.isPlaying)
                    {
                        var obj = new GameObject(typeof(T).Name);
                        instance = obj.AddComponent<T>();
                    }
                }

                // 初期化
                InitInstance();

                return instance;
            }
        }

        private static void InitInstance()
        {
            if (initInstance == null && instance != null && Application.isPlaying)
            {
                // シーン切り替えでもオブジェクトが消えないように設定
                DontDestroyOnLoad(instance.gameObject);

                // 初期化呼び出し
                var s = instance as CreateSingleton<T>;
                s.InitSingleton();

                initInstance = instance;
            }
        }

        /// <summary>
        /// インスタンスが存在する場合にTrueを返します
        /// </summary>
        /// <returns></returns>
        public static bool IsInstance()
        {
            return instance != null && isDestroy == false;
        }

        /// <summary>
        /// Awake()でのインスタンス設定
        /// </summary>
        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                InitInstance();
            }
            else if(instance != this)
            {
                // ２つ目のコンポーネントを発見
                var s = instance as CreateSingleton<T>;
                s.DuplicateDetection(this as T);

                // ２つ目のコンポーネントは破棄する
                Destroy(this.gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            // インスタンスクラスならば無効化フラグを立てる
            if (instance == this)
            {
                isDestroy = true;
            }
        }

        /// <summary>
        /// ２つ目の破棄されるコンポーネントを通知
        /// </summary>
        /// <param name="duplicate"></param>
        protected virtual void DuplicateDetection(T duplicate) { }

        /// <summary>
        /// 内部初期化
        /// </summary>
        protected abstract void InitSingleton();
    }
}
