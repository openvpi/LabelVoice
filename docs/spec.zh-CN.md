# LabelVoice 工程目录结构与格式规范

## 1 工程目录结构

每个工程（project）包含一个工程根目录及其内部所有被工程纳入管理的文件。一个典型的工程在磁盘上的存储结构如下：

```
|-- example    [1]
    |-- example.lvproj    [2]
    |-- example.lvconf    [3]
    |-- .gitignore    [4]
    |-- dictionaries    [5]
        |-- fe67_CHN    [6]
            |-- dict.txt    [7]
            |-- phoneset.txt    [8]
            |-- aligner    [9]
                |-- final.alimdl
                |-- final.mdl
                |-- final.occs
                |-- lda.mat
                |-- meta.json
                |-- tree
        |-- a61c_JPN
            ...
    |-- items    [10]
        |-- a54c548a_GuangNianZhiWai    [11]
            |-- a54c548a_GuangNianZhiWai.lvitem    [12]
            |-- 527a23a9_GanShouTingZaiWoFaDuanDeZhiJian.lvtext    [13]
            |-- 527a23a9_GanShouTingZaiWoFaDuanDeZhiJian.lvfeat    [14]
            |-- 88d4562a_RuHeShunJianDongJieShiJian.lvtext
            |-- 88d4562a_RuHeShunJianDongJieShiJian.lvfeat
        |-- 50deb91b_WoHuaiNianDe
            ...
        |-- 09692e28_PaoMo
            ...
        |-- 61cfc89a_BuWeiXia
            ...
        |-- 38db2292_ZheGanJue
            ...
    |-- wavs    [15]
        |-- a54c548a.wav    [16]
        |-- 38db2292.wav
```

[1] 工程根目录

[2] 工程描述文件，包含工程的属性

[3] 工程配置文件，包含工程中与用户有关的信息，例如设置项和已打开的标签页

[4] 用于在 git 版本控制时排除一些文件，包括模型文件、二进制文件和用户配置文件

[5] 存储各个语言的字典和强制对齐器

[6] 存储某一种语言的字典和强制对齐器，目录名格式为“随机编号_语言名称”，其中语言名称可由用户指定和修改

[7] 字典文件，每一行为一条音节到音素序列的对应规则，格式为“音节	音素1 音素2 ...”，例如“ba	b a”

[8] 音素表，包含可能出现的所有音素，每个音素记号之间用空格分隔，例如“b p m f ...”

[9] MFA 模型文件，具体包含内容需视模型部署情况而定

[10] 平铺存储所有项目（item）

[11] 存储单个项目，目录名格式为“随机编号_项目名称”，其中项目名称可由用户指定和修改

[12] 项目描述文件，包含项目的属性，文件名格式为“项目编号_项目名称.lvitem”

[13] 单个切片的标注文件，结构在类似于 TextGrid 的基础上添加绑定组，文件名格式为“随机编号_切片名称.lvtext”

[14] 单个切片的特征数据，由程序自动分析得到，包括 f0、力度、气声等信息，存储为二进制格式，文件名格式同上

[15] 存储波形文件的副本

[16] 波形文件，每个波形文件以其对应的项目的编号命名，仅在用户选择拷贝时存储于此处

## 2 工程描述文件（.lvproj）

文件示例如下：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Project>
    <Version>0.0.1</Version>
    <Name>LabelVoice Example Dataset</Name>
    <LabelSchema>
        <Layer Class="Sentence" Name="lyrics"/>
        <Layer Class="Grapheme" Name="pinyin" SubdivisionOf="0"/>
        <Layer Class="Pitch" Name="midi" SubdivisionOf="1"/>
        <Layer Class="Phoneme" Name="phone" SubdivisionOf="1"/>   
        <Layer Class="Duration" Name="dur" AlignedWith="3"/>
        <Layer Class="Custom" Name="slur" AlignedWith="3" ValueType="Category(2)"/>
    </LabelSchema>
    <Languages>
        <Language Id="fe67" Name="CHN" Dictionary="dict.txt" PhonemeSet="phoneset.txt" Aligner="aligner"/>
        <Language Id="a61c" Name="JPN" Dictionary="dict.txt" PhonemeSet="phoneset.txt"/>
    </Languages>
    <Speakers>
        <Speaker Id="32e9" Name="Opencpop"/>
        <Speaker Id="08cd" Name="ZhiBin"/>
    </Speakers>
    <ItemResources>
        <Item Id="a54c548a" Name="GuangNianZhiWai" Speaker="32e9" VisualPath=""/>
        <Item Id="50deb91b" Name="WoHuaiNianDe" Speaker="32e9" VisualPath="foo/bar"/>
        <Item Id="61cfc89a" Name="BuWeiXia" Speaker="08cd" VisualPath="foo"/>
        <Item Id="38db2292" Name="ZheGanJue" Speaker="08cd" VisualPath="foo/bar"/>
        <Placeholder Speaker="08cd" VisualPath="empty"/>
    </ItemResources>
</Project>
```

### 2.1 Version

`<Version>` 子标签存储工程文件格式的版本号。

### 2.2 Name

`<Name>` 子标签存储工程文件的名称，由用户指定并可修改。

### 2.3 LabelSchema

`<LabelSchema>` 子标签存储标注文件的层信息和绑定规则。层的下标从 0 开始，每一层（`<Layer>` 标签）具有以下属性：

- Class：层的类别，用于提供一些默认属性值预设，或作为一些自动流程的输入或输出
- Name：层的名称，由用户自定义
- SubdivisionOf：设置此属性代表本层是某一层中标记的细分，例如音素层是音节层的细分，一个音节标注能覆盖并恰好覆盖若干个音素标注
- AlignedWith：设置此属性代表本层与某一层完全对齐，即本层的所有标记与目标层的所有标记一对一头尾对齐
- ValueType：标注的数据类型

层的类别如下：

| 类别名称 |                          默认属性值                          |        特性        | 作为输入 | 作为输出 |
| :------: | :----------------------------------------------------------: | :----------------: | :------: | :------: |
| Sentence |                    ValueType 默认为 Text                     |         -          | 音素对齐 |    -     |
| Grapheme | SubdivisionOf 默认为第一个 Sentence 层；ValueType 默认为 Text | 对应词典规则的左侧 | 音高分析 | 音素对齐 |
| Phoneme  | SubdivisionOf 默认为第一个 Grapheme 层；ValueType 默认为 Text | 对应词典规则的右侧 |    -     | 音素对齐 |
|  Pitch   | Subdivision 默认为第一个 Grapheme 层；ValueType 默认为 Pitch |   使用国际谱音名   |    -     | 音高分析 |
| Duration |                   ValueType 默认为 Number                    | 标记文本绑定于时长 |    -     |    -     |
|  Custom  |                              -                               |         -          |    -     |    -     |

可用的数据类型如下：

- Text：文本类型，接受任意字符串输入
- Number：数值类型，接受整数或浮点数输入
- Pitch：音高类型，接受国际谱音名或 MIDI 编号输入
- Category(n)：类别类型，接受 0 ~ n-1 的数字输入

### 2.3 Languages

`<Languages>` 子标签中存储了工程中定义的所有语言。每种语言具有以下属性：

- Id：语言的编号，为 4 位随机十六进制字符串，一旦确定不可变更
- Name：语言的名称，由用户设置并可修改
- Dictionary：该语言的词典路径，接受 MFA 词典格式，用于音素对齐、Grapheme 层和 Phoneme 层的标注校验和 Grapheme 层的输入补全
- PhoneSet：该语言的音素表路径，包含该语言的所有音素，用于 Phoneme 层的输入补全，可由词典自动生成
- Aligner：该语言的音素对齐器目录，通常为 MFA

### 2.4 Speakers

`<Speakers>` 子标签中存储了工程中定义的所有说话人，每位说话人具有以下属性：

- Id：说话人的编号，为 4 位随机十六进制字符串，一旦确定不可变更
- Name：说话人的名称，由用户设置并可修改

### 2.5 ItemResources

`<ItemResources>` 子标签中存储了纳入工程管理的所有项目。该子标签中有两类元素：

`<Item>` 标签代表的是一个真实的项目，具有以下属性：

- Id：项目的编号，为 8 位随机十六进制字符串，一旦确定不可变更
- Name：项目的名称，由用户设置并可修改
- Speaker：该项目所属的说话人的编号
- VisualPath：该项目在资源浏览器中所处的虚拟路径

`<Placeholder>` 标签代表的是一个占位符，用于代表一个不包含任何真实项目或其他占位符的空虚拟路径，具有以下属性：

- Speaker：该占位符所属的说话人的编号
- VisualPath：该占位符所代表的虚拟路径

