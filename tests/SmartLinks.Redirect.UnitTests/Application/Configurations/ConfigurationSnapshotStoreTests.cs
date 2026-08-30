using SmartLinks.Contracts.Configurations;
using SmartLinks.Redirect.Application.Configurations;
using SmartLinks.RuleEngine.Conditions;

namespace SmartLinks.Redirect.UnitTests.Application.Configurations;

public sealed class ConfigurationSnapshotStoreTests
{
    /// <summary>
    /// Проверяет замену полного snapshot и доступность скомпилированной конфигурации по slug
    /// </summary>
    [Fact]
    public void ReplaceSnapshotMakesCompiledConfigurationAvailableBySlug()
    {
        var conditionCompiler = new ConditionCompiler([new CountryConditionFactory()]);
        var conditionDslCompiler = new ConditionDslCompiler(conditionCompiler);
        var store = new ConfigurationSnapshotStore(conditionDslCompiler);
        var snapshot = new PublishedSmartLinksSnapshot(
            7,
            [
                new SmartLinkConfigurationSnapshot(
                    Guid.NewGuid(),
                    "summer-sale",
                    "https://example.com/default",
                    true,
                    [
                        new SmartLinkRuleSnapshot(
                            10,
                            true,
                            "https://example.com/netherlands",
                            """
                            {
                              "dslVersion": 1,
                              "condition": {
                                "type": "country",
                                "parameters": {
                                  "countryCode": "NL"
                                }
                              }
                            }
                            """)
                    ])
            ]);

        store.ReplaceSnapshot(snapshot);

        Assert.True(store.TryGetBySlug("summer-sale", out var configuration));
        Assert.NotNull(configuration);
        Assert.Equal(7L, store.Revision);
        Assert.True(configuration.IsActive);
        Assert.Equal("https://example.com/default", configuration.DefaultUrl);

        var rule = Assert.Single(configuration.Rules);

        Assert.Equal(10, rule.Priority);
        Assert.True(rule.IsEnabled);
        Assert.Equal("https://example.com/netherlands", rule.TargetUrl);
        Assert.IsType<CountryCondition>(rule.Condition);
    }

    /// <summary>
    /// Проверяет начальную нулевую ревизию и отсутствие конфигураций
    /// </summary>
    [Fact]
    public void NewStoreHasZeroRevisionAndNoConfigurations()
    {
        var store = CreateStore();

        Assert.Equal(0L, store.Revision);
        Assert.False(store.TryGetBySlug("missing-link", out var configuration));
        Assert.Null(configuration);
    }

    /// <summary>
    /// Проверяет регистронезависимый поиск конфигурации по slug
    /// </summary>
    [Fact]
    public void TryGetBySlugUsesCaseInsensitiveComparison()
    {
        var store = CreateStore();
        store.ReplaceSnapshot(CreateSnapshot(
            1,
            CreateConfiguration("Summer-Sale")));

        Assert.True(store.TryGetBySlug("summer-sale", out var configuration));
        Assert.NotNull(configuration);
    }

    /// <summary>
    /// Проверяет удаление отсутствующих в новом полном snapshot конфигураций
    /// </summary>
    [Fact]
    public void ReplaceSnapshotRemovesConfigurationsMissingFromNewSnapshot()
    {
        var store = CreateStore();
        store.ReplaceSnapshot(CreateSnapshot(
            1,
            CreateConfiguration("first-link")));

        store.ReplaceSnapshot(CreateSnapshot(
            2,
            CreateConfiguration("second-link")));

        Assert.Equal(2L, store.Revision);
        Assert.False(store.TryGetBySlug("first-link", out _));
        Assert.True(store.TryGetBySlug("second-link", out _));
    }

    /// <summary>
    /// Проверяет сохранение предыдущей модели при ошибке компиляции нового snapshot
    /// </summary>
    [Fact]
    public void ReplaceSnapshotWithInvalidDslPreservesCurrentSnapshot()
    {
        var store = CreateStore();
        store.ReplaceSnapshot(CreateSnapshot(
            5,
            CreateConfiguration("stable-link")));

        var invalidConfiguration = CreateConfiguration(
            "invalid-link",
            """
            {
              "dslVersion": 2,
              "condition": {
                "type": "country",
                "parameters": {
                  "countryCode": "NL"
                }
              }
            }
            """);

        Assert.Throws<InvalidOperationException>(() =>
            store.ReplaceSnapshot(CreateSnapshot(6, invalidConfiguration)));

        Assert.Equal(5L, store.Revision);
        Assert.True(store.TryGetBySlug("stable-link", out _));
        Assert.False(store.TryGetBySlug("invalid-link", out _));
    }

    /// <summary>
    /// Проверяет игнорирование полного snapshot со старой ревизией
    /// </summary>
    [Fact]
    public void ReplaceSnapshotIgnoresOlderRevision()
    {
        var store = CreateStore();
        store.ReplaceSnapshot(CreateSnapshot(
            5,
            CreateConfiguration("current-link")));

        store.ReplaceSnapshot(CreateSnapshot(
            4,
            CreateConfiguration("older-link")));

        Assert.Equal(5L, store.Revision);
        Assert.True(store.TryGetBySlug("current-link", out _));
        Assert.False(store.TryGetBySlug("older-link", out _));
    }

    /// <summary>
    /// Проверяет игнорирование повторного полного snapshot той же ревизии
    /// </summary>
    [Fact]
    public void ReplaceSnapshotIgnoresRepeatedRevision()
    {
        var store = CreateStore();
        store.ReplaceSnapshot(CreateSnapshot(
            5,
            CreateConfiguration("current-link")));

        store.ReplaceSnapshot(CreateSnapshot(
            5,
            CreateConfiguration("repeated-link")));

        Assert.Equal(5L, store.Revision);
        Assert.True(store.TryGetBySlug("current-link", out _));
        Assert.False(store.TryGetBySlug("repeated-link", out _));
    }

    /// <summary>
    /// Проверяет независимость read-модели от изменяемых коллекций исходного snapshot
    /// </summary>
    [Fact]
    public void ReplaceSnapshotCreatesIndependentModelFromMutableSourceCollections()
    {
        var store = CreateStore();
        List<SmartLinkRuleSnapshot> sourceRules =
        [
            new SmartLinkRuleSnapshot(
                10,
                true,
                "https://example.com/netherlands",
                CreateDsl("NL"))
        ];
        List<SmartLinkConfigurationSnapshot> sourceConfigurations =
        [
            new SmartLinkConfigurationSnapshot(
                Guid.NewGuid(),
                "stable-link",
                "https://example.com/default",
                true,
                sourceRules)
        ];

        store.ReplaceSnapshot(new PublishedSmartLinksSnapshot(
            1,
            sourceConfigurations));

        sourceRules.Clear();
        sourceConfigurations.Clear();

        Assert.True(store.TryGetBySlug("stable-link", out var configuration));
        Assert.NotNull(configuration);
        Assert.Single(configuration.Rules);
    }

    /// <summary>
    /// Проверяет добавление последовательного изменения и продвижение текущей ревизии
    /// </summary>
    [Fact]
    public void ApplyChangesAddsConfigurationAndAdvancesRevision()
    {
        var store = CreateStore();
        store.ReplaceSnapshot(CreateSnapshot(
            3,
            CreateConfiguration("existing-link")));
        IReadOnlyList<ConfigurationChange> changes =
        [
            new ConfigurationChange(
                4,
                CreateConfiguration("new-link"))
        ];

        store.ApplyChanges(changes);

        Assert.Equal(4L, store.Revision);
        Assert.True(store.TryGetBySlug("existing-link", out _));
        Assert.True(store.TryGetBySlug("new-link", out _));
    }

    /// <summary>
    /// Проверяет применение нескольких непрерывных изменений по порядку ревизий
    /// </summary>
    [Fact]
    public void ApplyChangesAppliesContiguousBatchInRevisionOrder()
    {
        var store = CreateStore();
        store.ReplaceSnapshot(CreateSnapshot(
            3,
            CreateConfiguration("existing-link")));
        IReadOnlyList<ConfigurationChange> changes =
        [
            new ConfigurationChange(
                4,
                CreateConfiguration("fourth-link")),
            new ConfigurationChange(
                5,
                CreateConfiguration("fifth-link"))
        ];

        store.ApplyChanges(changes);

        Assert.Equal(5L, store.Revision);
        Assert.True(store.TryGetBySlug("existing-link", out _));
        Assert.True(store.TryGetBySlug("fourth-link", out _));
        Assert.True(store.TryGetBySlug("fifth-link", out _));
    }

    /// <summary>
    /// Проверяет замену опубликованной конфигурации существующей ссылки
    /// </summary>
    [Fact]
    public void ApplyChangesUpdatesExistingConfiguration()
    {
        var store = CreateStore();
        var id = Guid.NewGuid();
        store.ReplaceSnapshot(CreateSnapshot(
            1,
            CreateConfiguration(
                "updated-link",
                id: id,
                defaultUrl: "https://example.com/old")));
        IReadOnlyList<ConfigurationChange> changes =
        [
            new ConfigurationChange(
                2,
                CreateConfiguration(
                    "updated-link",
                    id: id,
                    defaultUrl: "https://example.com/new"))
        ];

        store.ApplyChanges(changes);

        Assert.Equal(2L, store.Revision);
        Assert.True(store.TryGetBySlug("updated-link", out var configuration));
        Assert.NotNull(configuration);
        Assert.Equal("https://example.com/new", configuration.DefaultUrl);
    }

    /// <summary>
    /// Проверяет удаление предыдущего slug при переименовании опубликованной ссылки
    /// </summary>
    [Fact]
    public void ApplyChangesRemovesPreviousSlugWhenConfigurationIsRenamed()
    {
        var store = CreateStore();
        var id = Guid.NewGuid();
        store.ReplaceSnapshot(CreateSnapshot(
            1,
            CreateConfiguration(
                "old-slug",
                id: id)));
        IReadOnlyList<ConfigurationChange> changes =
        [
            new ConfigurationChange(
                2,
                CreateConfiguration(
                    "new-slug",
                    id: id))
        ];

        store.ApplyChanges(changes);

        Assert.Equal(2L, store.Revision);
        Assert.False(store.TryGetBySlug("old-slug", out _));
        Assert.True(store.TryGetBySlug("new-slug", out _));
    }

    /// <summary>
    /// Проверяет игнорирование изменений старых и уже применённых ревизий
    /// </summary>
    [Fact]
    public void ApplyChangesIgnoresOldAndRepeatedRevisions()
    {
        var store = CreateStore();
        store.ReplaceSnapshot(CreateSnapshot(
            5,
            CreateConfiguration("current-link")));
        IReadOnlyList<ConfigurationChange> changes =
        [
            new ConfigurationChange(
                4,
                CreateConfiguration("older-link")),
            new ConfigurationChange(
                5,
                CreateConfiguration("repeated-link"))
        ];

        store.ApplyChanges(changes);

        Assert.Equal(5L, store.Revision);
        Assert.True(store.TryGetBySlug("current-link", out _));
        Assert.False(store.TryGetBySlug("older-link", out _));
        Assert.False(store.TryGetBySlug("repeated-link", out _));
    }

    /// <summary>
    /// Проверяет игнорирование повторной ревизии внутри одного пакета изменений
    /// </summary>
    [Fact]
    public void ApplyChangesIgnoresRepeatedRevisionWithinBatch()
    {
        var store = CreateStore();
        store.ReplaceSnapshot(CreateSnapshot(
            5,
            CreateConfiguration("current-link")));
        IReadOnlyList<ConfigurationChange> changes =
        [
            new ConfigurationChange(
                6,
                CreateConfiguration("sixth-link")),
            new ConfigurationChange(
                6,
                CreateConfiguration("repeated-sixth-link"))
        ];

        store.ApplyChanges(changes);

        Assert.Equal(6L, store.Revision);
        Assert.True(store.TryGetBySlug("sixth-link", out _));
        Assert.False(store.TryGetBySlug("repeated-sixth-link", out _));
    }

    /// <summary>
    /// Проверяет отклонение всего пакета при пропуске ожидаемой ревизии
    /// </summary>
    [Fact]
    public void ApplyChangesWithRevisionGapPreservesCurrentSnapshot()
    {
        var store = CreateStore();
        store.ReplaceSnapshot(CreateSnapshot(
            5,
            CreateConfiguration("current-link")));
        IReadOnlyList<ConfigurationChange> changes =
        [
            new ConfigurationChange(
                6,
                CreateConfiguration("sixth-link")),
            new ConfigurationChange(
                8,
                CreateConfiguration("eighth-link"))
        ];

        Assert.Throws<InvalidOperationException>(() =>
            store.ApplyChanges(changes));

        Assert.Equal(5L, store.Revision);
        Assert.True(store.TryGetBySlug("current-link", out _));
        Assert.False(store.TryGetBySlug("sixth-link", out _));
        Assert.False(store.TryGetBySlug("eighth-link", out _));
    }

    /// <summary>
    /// Проверяет сохранение текущей модели при ошибке компиляции изменения
    /// </summary>
    [Fact]
    public void ApplyChangesWithInvalidDslPreservesCurrentSnapshot()
    {
        var store = CreateStore();
        store.ReplaceSnapshot(CreateSnapshot(
            5,
            CreateConfiguration("current-link")));
        IReadOnlyList<ConfigurationChange> changes =
        [
            new ConfigurationChange(
                6,
                CreateConfiguration("sixth-link")),
            new ConfigurationChange(
                7,
                CreateConfiguration(
                    "invalid-link",
                    """
                    {
                      "dslVersion": 2,
                      "condition": {
                        "type": "country",
                        "parameters": {
                          "countryCode": "NL"
                        }
                      }
                    }
                    """))
        ];

        Assert.Throws<InvalidOperationException>(() =>
            store.ApplyChanges(changes));

        Assert.Equal(5L, store.Revision);
        Assert.True(store.TryGetBySlug("current-link", out _));
        Assert.False(store.TryGetBySlug("sixth-link", out _));
        Assert.False(store.TryGetBySlug("invalid-link", out _));
    }

    /// <summary>
    /// Проверяет отсутствие изменений при пустом пакете
    /// </summary>
    [Fact]
    public void ApplyChangesWithEmptyCollectionDoesNotChangeSnapshot()
    {
        var store = CreateStore();
        store.ReplaceSnapshot(CreateSnapshot(
            5,
            CreateConfiguration("current-link")));

        store.ApplyChanges(Array.Empty<ConfigurationChange>());

        Assert.Equal(5L, store.Revision);
        Assert.True(store.TryGetBySlug("current-link", out _));
    }

    /// <summary>
    /// Создаёт хранилище с поддержкой тестового условия страны
    /// </summary>
    private static ConfigurationSnapshotStore CreateStore()
    {
        var conditionCompiler = new ConditionCompiler([new CountryConditionFactory()]);
        var conditionDslCompiler = new ConditionDslCompiler(conditionCompiler);

        return new ConfigurationSnapshotStore(conditionDslCompiler);
    }

    /// <summary>
    /// Создаёт полный snapshot указанной ревизии
    /// </summary>
    private static PublishedSmartLinksSnapshot CreateSnapshot(
        long revision,
        params SmartLinkConfigurationSnapshot[] configurations)
    {
        return new PublishedSmartLinksSnapshot(revision, configurations);
    }

    /// <summary>
    /// Создаёт опубликованную конфигурацию с одним правилом страны
    /// </summary>
    private static SmartLinkConfigurationSnapshot CreateConfiguration(
        string slug,
        string? conditionDsl = null,
        Guid? id = null,
        string defaultUrl = "https://example.com/default",
        string targetUrl = "https://example.com/netherlands")
    {
        return new SmartLinkConfigurationSnapshot(
            id ?? Guid.NewGuid(),
            slug,
            defaultUrl,
            true,
            [
                new SmartLinkRuleSnapshot(
                    10,
                    true,
                    targetUrl,
                    conditionDsl ?? CreateDsl("NL"))
            ]);
    }

    /// <summary>
    /// Создаёт допустимый DSL условия страны
    /// </summary>
    private static string CreateDsl(string countryCode)
    {
        return $$"""
            {
              "dslVersion": 1,
              "condition": {
                "type": "country",
                "parameters": {
                  "countryCode": "{{countryCode}}"
                }
              }
            }
            """;
    }
}