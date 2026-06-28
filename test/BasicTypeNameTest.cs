// SPDX-FileCopyrightText: (C) 2026 dffdff2423 <dffdff2423@gmail.com>
//
// SPDX-License-Identifier: GPL-3.0-only

using YamlWarrior.Robust.Utilities;

namespace tests;

public sealed class BasicTypeNameTest {
    [Test]
    public void Parse_string() {
        var ty = new CSharpTypeName("System.String");
        using (Assert.EnterMultipleScope()) {
            Assert.That(ty.TypePath, Is.EqualTo("System.String"));
            Assert.That(ty.IsArray, Is.False);
            Assert.That(ty.NumGenerics, Is.EqualTo(0));
            Assert.That(ty.GenericParameters, Is.Null);
        }
    }

    [Test]
    public void Parse_string_array() {
        var ty = new CSharpTypeName("System.String[]");
        using (Assert.EnterMultipleScope()) {
            Assert.That(ty.TypePath, Is.EqualTo("System.String"));
            Assert.That(ty.IsArray, Is.True);
            Assert.That(ty.NumGenerics, Is.EqualTo(0));
            Assert.That(ty.GenericParameters, Is.Null);
        }
    }

    [Test]
    public void Parse_unparamaterized_generic() {
        var ty = new CSharpTypeName("My.Generic`2");
        using (Assert.EnterMultipleScope()) {
            Assert.That(ty.TypePath, Is.EqualTo("My.Generic"));
            Assert.That(ty.IsArray, Is.False);
            Assert.That(ty.NumGenerics, Is.EqualTo(2));
            Assert.That(ty.GenericParameters, Is.Null);
        }
    }

    [Test]
    public void Parse_dict() {
        var ty = new CSharpTypeName(
            "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]");
        using (Assert.EnterMultipleScope()) {
            Assert.That(ty.TypePath, Is.EqualTo("System.Collections.Generic.Dictionary"));
            Assert.That(ty.IsArray, Is.False);
            Assert.That(ty.NumGenerics, Is.EqualTo(2));
            Assert.That(ty.GenericParameters,
                Is.EquivalentTo([new CSharpTypeName("System.String"), new CSharpTypeName("System.String")]));
        }
    }

    [Test]
    public void Parse_nested() {
        var ty = new CSharpTypeName(
            "System.Collections.Generic.List`1+Enumerator[[Robust.Shared.GameObjects.EntityUid, Robust.Shared, Version=277.0.0.0, Culture=neutral, PublicKeyToken=null]]");
        using (Assert.EnterMultipleScope()) {
            Assert.That(ty.TypePath, Is.EqualTo("System.Collections.Generic.List`1+Enumerator"));
            Assert.That(ty.IsArray, Is.False);
            Assert.That(ty.NumGenerics, Is.EqualTo(1));
            Assert.That(ty.GenericParameters,
                Is.EquivalentTo([new CSharpTypeName("Robust.Shared.GameObjects.EntityUid")]));
        }
    }
}
