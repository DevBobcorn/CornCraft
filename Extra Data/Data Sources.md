Data files in this folder come from various sources:

File                                                      | Source
---                                                       | :---:
app_lang/\<lang>.lang                                     | Manually Maintained
blocks/block_entity_types-\<version>.json                 | Block Entity Data Exporter
blocks/blocks-\<version>.json                             | Block Data Exporter
entities/entity_types-\<version>.json                     | Entity Data Exporter
items/items-\<version>.json                               | Item Data Exporter / Item Component Data Exporter
protos/protodef-\<proto_version>.json                     | [Minecraft Data](https://github.com/PrismarineJS/minecraft-data), used only for packet preview
block_colors.json                                         | Manually Maintained
block_interaction.json                                    | Manually Maintained
block_render_type.json                                    | Manually Maintained
entity_bedrock_model_render_type.json                     | Manually Maintained (Planned for Removal)
item_colors.json                                          | Manually Maintained
sprite_types.json                                         | Manually Maintained (Vanilla Doesn't Have These)
versions.json                                             | Manually Maintained

Here is an example of [Vanilla Data Exporters](https://gist.github.com/DevBobcorn/bc5e57c2659d3b6b166974d7c781a88c) used for generating the data listed above.
