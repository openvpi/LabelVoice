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

方案一：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Project>
    <Items>
        <Item Id="a54c548a" Name="GuangNianZhiWai" Speaker="32e9" VisualPath=""/>
        <Item Id="50deb91b" Name="WoHuaiNianDe" Speaker="32e9" VisualPath="foo/bar"/>
        <Item Id="61cfc89a" Name="BuWeiXia" Speaker="08cd" VisualPath="foo"/>
        <Item Id="38db2292" Name="ZheGanJue" Speaker="08cd" VisualPath="foo/bar"/>
    </Items>
</Project>
```

方案二：

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Project>
    <ItemResources>
        <Speaker Id="32e9" Name="Opencpop">
            <Item Id="a54c548a" Name="GuangNianZhiWai"/>
            <Folder Name="foo">
                <Folder Name="bar">
                    <Item Id="50deb91b" Name="WoHuaiNianDe"/>
                </Folder>
            </Folder>
        </Speaker>
        <Speaker Id="08cd" Name="ZhiBin">
            <Folder Name="foo">
                <Item Id="61cfc89a" Name="BuWeiXia"/>
                <Folder Name="bar">
                    <Item Id="38db2292" Name="ZheGanJue"/>
                </Folder>
            </Folder>
            <Folder Name="empty">
            </Folder>
        </Speaker>
    </ItemResources>
</Project>
```

