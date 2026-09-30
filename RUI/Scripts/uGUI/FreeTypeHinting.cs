namespace REDIZIT.RUI
{
	public enum FreeTypeHinting
	{
		Normal = 0x0,          // Стандартный байткод шрифта
		Light = 0x10000,       // FT_LOAD_TARGET_LIGHT (рекомендуется для 9-12px)
		AutoHint = 0x20        // Принудительный автохинтинг
	}
}